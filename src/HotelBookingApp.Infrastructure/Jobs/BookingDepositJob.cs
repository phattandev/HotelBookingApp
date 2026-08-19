using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBookingApp.Infrastructure.Jobs;

/// <summary>
/// Hangfire Job xử lý tự động:
/// 1. Hủy đơn quá hạn chưa thanh toán cọc.
/// 2. Gửi email nhắc nhở khách chưa cọc.
/// </summary>
public class BookingDepositJob
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingDepositJob> _logger;

    public BookingDepositJob(IApplicationDbContext context, IEmailService emailService, ILogger<BookingDepositJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Tự động hủy đơn quá hạn thanh toán cọc (quá DepositDeadline mà PaymentStatus = Unpaid).
    /// Chạy mỗi 30 phút.
    /// </summary>
    public async Task AutoCancelUnpaidBookingsAsync()
    {
        var now = DateTime.UtcNow;

        var overdueBookings = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Hotel)
            .Include(b => b.Items).ThenInclude(i => i.RoomType)
            .Where(b =>
                b.PaymentStatus == PaymentStatus.Unpaid &&
                b.DepositDeadline != null &&
                b.DepositDeadline < now &&
                b.Status != BookingStatus.Cancelled &&
                b.Status != BookingStatus.Completed)
            .ToListAsync();

        if (!overdueBookings.Any())
        {
            _logger.LogInformation("[BookingDepositJob] Không có đơn nào cần tự hủy.");
            return;
        }

        foreach (var booking in overdueBookings)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelReason = "Tự động hủy: Quá hạn thanh toán đặt cọc.";
            booking.CancelledAt = now;
            booking.UpdatedAt = now;
        }

        // 1. LƯU DATABASE TRƯỚC: Đảm bảo giao dịch an toàn
        await _context.SaveChangesAsync(CancellationToken.None);

        // 2. GỬI EMAIL SAU: Bọc try-catch để nếu 1 email lỗi không làm chết cả job
        foreach (var booking in overdueBookings)
        {
            try
            {
                await _emailService.SendBookingCancelledAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BookingDepositJob] Lỗi gửi email hủy cho booking {Id}", booking.Id);
            }
        }

        _logger.LogInformation("[BookingDepositJob] Đã tự động hủy {Count} đơn do không thanh toán cọc.", overdueBookings.Count);
    }

    /// <summary>
    /// Gửi email nhắc nhở khách hàng chưa thanh toán cọc cho đơn còn trong hạn.
    /// Chạy mỗi 60 phút.
    /// </summary>
    public async Task SendDepositReminderEmailsAsync()
    {
        var now = DateTime.UtcNow;

        // Các đơn đã Approved, chưa cọc, chưa hủy, còn trong hạn
        var pendingDeposits = await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Hotel)
            .Include(b => b.Items).ThenInclude(i => i.RoomType)
            .Where(b =>
                b.PaymentStatus == PaymentStatus.Unpaid &&
                b.Status == BookingStatus.Approved &&
                b.DepositDeadline != null &&
                b.DepositDeadline > now &&
                b.DepositDeadline <= now.AddHours(3)) // Chỉ nhắc khi gần hết hạn (trong 3h)
            .ToListAsync();

        _logger.LogInformation("[BookingDepositJob] Gửi nhắc nhở cho {Count} đơn chưa cọc.", pendingDeposits.Count);

        foreach (var booking in pendingDeposits)
        {
            try
            {
                await _emailService.SendDepositReminderAsync(booking);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BookingDepositJob] Lỗi gửi email nhắc cọc cho booking {Id}", booking.Id);
            }
        }
    }
}
