using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HotelBookingApp.Infrastructure.Jobs;

/// <summary>
/// Hangfire Recurring Job: tự động chuyển trạng thái đơn từ Confirmed → Completed
/// sau khi quá giờ CheckOut (12:00 trưa giờ VN).
/// Chạy định kỳ mỗi giờ.
/// </summary>
public class BookingCompletionJob
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<BookingCompletionJob> _logger;

    public BookingCompletionJob(IApplicationDbContext context, IEmailService emailService, ILogger<BookingCompletionJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Tìm tất cả booking Confirmed có CheckOutDate < hôm nay và cập nhật thành Completed.
    /// </summary>
    public async Task ExecuteAsync()
    {
        var vnNow = DateTime.UtcNow.AddHours(7);
        var vnToday = DateOnly.FromDateTime(vnNow);

        bool isPastNoon = vnNow.Hour >= 12;

        var expiredBookings = await _context.Bookings
            .Include(b => b.Hotel)
            .Where(b => b.Status == BookingStatus.Confirmed &&
                        (b.CheckOutDate < vnToday || (b.CheckOutDate == vnToday && isPastNoon)))
            .ToListAsync();

        if (!expiredBookings.Any())
        {
            _logger.LogInformation("[BookingCompletionJob] Không có đơn nào cần cập nhật lúc ({Time}).", vnNow);
            return;
        }

        foreach (var booking in expiredBookings)
        {
            booking.Status = BookingStatus.Completed;
            booking.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(CancellationToken.None);

        // Gửi email cảm ơn sau checkout
        foreach (var booking in expiredBookings)
        {
            try
            {
                var hotelName = booking.Hotel?.Name ?? "Khách sạn";
                await _emailService.SendPostCheckoutThankYouAsync(
                    booking.GuestEmail,
                    booking.GuestName,
                    booking.Id.ToString(),
                    hotelName,
                    booking.CheckOutDate);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[BookingCompletionJob] Lỗi gửi email cho booking {Id}", booking.Id);
            }
        }

        _logger.LogInformation(
            "[BookingCompletionJob] Đã tự động hoàn thành {Count} đơn đặt phòng (CheckOut trước {Time}).",
            expiredBookings.Count, vnNow);
    }
}
