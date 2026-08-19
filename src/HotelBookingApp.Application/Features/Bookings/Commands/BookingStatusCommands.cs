using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Bookings.Commands
{
    /// <summary>
    /// Command cho phép khách hàng tự hủy đơn của mình.
    /// Cho phép hủy cả đơn Pending và Confirmed.
    /// Nếu hủy Confirmed trong mốc HoursBeforeCheckIn → tính phạt theo CancellationPolicy.
    /// </summary>
    public class CancelBookingCommand : IRequest<Response<CancelBookingResultDto>>
    {
        public Guid BookingId { get; set; }
        public Guid CustomerId { get; set; }    // Set từ JWT để xác minh quyền sở hữu
        /// <summary>Lý do hủy — bắt buộc phải nhập.</summary>
        public string CancelReason { get; set; } = null!;
    }

    /// <summary>Kết quả hủy đơn, bao gồm thông tin hoàn tiền để hiển thị cho khách.</summary>
    public class CancelBookingResultDto
    {
        public string Message { get; set; } = null!;
        public decimal TotalPrice { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public bool HasPenalty => PenaltyAmount > 0;
    }

    public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, Response<CancelBookingResultDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        public CancelBookingCommandHandler(IApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<Response<CancelBookingResultDto>> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Items).ThenInclude(i => i.RoomType)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                throw new ApiException("Không tìm thấy đơn đặt phòng.");

            // Kiểm tra quyền sở hữu: chỉ chính khách hàng đặt mới được hủy
            if (booking.CustomerId != request.CustomerId)
                throw new ApiException("Bạn không có quyền hủy đơn này.");

            // Cho phép hủy khi đơn đang Pending, Approved (chưa cọc) hoặc Confirmed
            if (booking.Status != BookingStatus.Pending 
                && booking.Status != BookingStatus.Approved
                && booking.Status != BookingStatus.Confirmed)
                throw new ApiException($"Không thể hủy đơn đang ở trạng thái '{booking.Status}'. Chỉ hủy được đơn chưa hoàn thành.");

            // Tính phạt nếu đơn đang Confirmed và hủy trong mốc thời gian của policy
            decimal penaltyAmount = 0;
            decimal refundAmount = booking.TotalPrice;

            if (booking.Status == BookingStatus.Confirmed && booking.CancellationPolicyId.HasValue)
            {
                var policy = await _context.HotelCancellationPolicies
                    .FirstOrDefaultAsync(p => p.Id == booking.CancellationPolicyId.Value, cancellationToken);

                if (policy != null)
                {
                    // Tính số giờ còn lại đến check-in (mặc định 14:00)
                    var checkInDateTime = booking.CheckInDate.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc);
                    var hoursLeft = (checkInDateTime - DateTime.UtcNow).TotalHours;

                    // Nếu hủy trong mốc → áp dụng phạt trên số tiền cọc (DepositAmount)
                    if (hoursLeft < policy.HoursBeforeCheckIn)
                    {
                        var penaltyPercent = booking.PenaltyPercentageSnapshot ?? policy.PenaltyPercentage;
                        penaltyAmount = booking.DepositAmount * (penaltyPercent / 100m);
                    }
                }
            }

            var cancelledAt = DateTime.UtcNow;
            booking.Status = BookingStatus.Cancelled;
            booking.CancelReason = request.CancelReason;
            booking.CancelledAt = cancelledAt;
            booking.UpdatedAt = cancelledAt;

            // Xử lý hoàn cọc (nếu đã thanh toán cọc)
            decimal refundDepositAmount = 0;
            if (booking.PaymentStatus == PaymentStatus.Paid)
            {
                refundDepositAmount = booking.DepositAmount - penaltyAmount;

                if (refundDepositAmount > 0)
                {
                    booking.PaymentStatus = PaymentStatus.Refunded;
                    booking.RefundAmount = refundDepositAmount;
                    
                    await _emailService.SendDepositRefundedAsync(booking);
                }
                else
                {
                    booking.RefundAmount = 0;
                }
                booking.PenaltyAmount = penaltyAmount;
            }
            else
            {
                booking.PenaltyAmount = 0; // Chưa thanh toán cọc thì không có tiền phạt thực tế
                booking.RefundAmount = 0;
            }

            await _emailService.SendBookingCancelledAsync(booking);

            await _context.SaveChangesAsync(cancellationToken);

            var depositMsg = refundDepositAmount > 0
                ? $" Tiền cọ {refundDepositAmount:N0}đ sẽ được hoàn trả trong vòng 3-5 ngày làm việc."
                : (booking.PaymentStatus == PaymentStatus.Paid ? " Tiền cọc không được hoàn do hủy sau mốc chính sách." : "");

            var result = new CancelBookingResultDto
            {
                Message = penaltyAmount > 0
                    ? $"Đã hủy đơn. Do hủy trong mốc quy định, bạn bị phạt {penaltyAmount:N0}đ.{depositMsg}"
                    : $"Đã hủy đơn đặt phòng thành công.{depositMsg}",
                TotalPrice = booking.TotalPrice,
                PenaltyAmount = penaltyAmount,
                RefundAmount = refundDepositAmount
            };

            return new Response<CancelBookingResultDto>(result, result.Message);
        }
    }

    public class CancelBookingCommandValidator : AbstractValidator<CancelBookingCommand>
    {
        public CancelBookingCommandValidator()
        {
            RuleFor(x => x.CancelReason)
                .NotEmpty().WithMessage("Vui lòng nhập lý do hủy đơn.")
                .MinimumLength(5).WithMessage("Lý do hủy quá ngắn, vui lòng mô tả rõ hơn.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Command cho Manager xác nhận hoặc từ chối đơn đặt phòng
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Command để Manager cập nhật trạng thái đơn đặt phòng (Xác nhận hoặc Từ chối).
    /// Chỉ Manager (RoleInHotel = "manager") mới được phép thực hiện.
    /// </summary>
    public class UpdateBookingStatusCommand : IRequest<Response<string>>
    {
        public Guid BookingId { get; set; }
        public Guid ManagerId { get; set; }     // Set từ JWT
        /// <summary>Hành động: "confirm" hoặc "reject".</summary>
        public string Action { get; set; } = null!;
        /// <summary>Bắt buộc khi Action = "reject".</summary>
        public string? CancelReason { get; set; }
    }

    public class UpdateBookingStatusCommandHandler : IRequestHandler<UpdateBookingStatusCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        public UpdateBookingStatusCommandHandler(IApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<Response<string>> Handle(UpdateBookingStatusCommand request, CancellationToken cancellationToken)
        {
            // Kiểm tra Manager có phân công quản lý khách sạn và có role "manager" không
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.UserId == request.ManagerId &&
                    a.RoleInHotel == "manager" &&   // QUAN TRỌNG: chỉ manager mới confirm được
                    a.IsActive,
                    cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không có quyền quản lý đơn đặt phòng. Chỉ Quản lý khách sạn mới được thực hiện thao tác này.");

            // Tìm đơn đặt phòng thuộc khách sạn Manager đang quản lý
            var booking = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Items)
                    .ThenInclude(i => i.RoomType)
                .FirstOrDefaultAsync(b =>
                    b.Id == request.BookingId &&
                    b.HotelId == assignment.HotelId,
                    cancellationToken);

            if (booking == null)
                throw new ApiException("Không tìm thấy đơn đặt phòng thuộc khách sạn này.");

            if (booking.Status != BookingStatus.Pending)
                throw new ApiException("Chỉ có thể xử lý đơn đang ở trạng thái chờ xác nhận.");

            if (request.Action == "approve")
            {
                booking.Status = BookingStatus.Approved;
                
                var now = DateTime.UtcNow;

                // Lấy chính sách đặt cọc của khách sạn (nếu có)
                var depositPolicy = await _context.HotelDepositPolicies
                    .FirstOrDefaultAsync(p => p.HotelId == assignment.HotelId && p.IsActive, cancellationToken);

                // Số giờ trước ngày check-in mà khách phải cọc xong (mặc định 24h nếu chưa có policy)
                var hoursBeforeCheckIn = depositPolicy?.HoursBeforeCheckIn ?? 24;

                // Thời điểm 14:00 ngày check-in (UTC)
                var checkInTime = booking.CheckInDate.ToDateTime(new TimeOnly(14, 0), DateTimeKind.Utc);

                // Hạn chót bình thường = 14:00 ngày check-in − hoursBeforeCheckIn
                var normalDeadline = checkInTime.AddHours(-hoursBeforeCheckIn);

                if (normalDeadline <= now)
                {
                    // Đặt phòng sát giờ (quá mốc bình thường) → thời gian ân hạn 4 giờ
                    booking.DepositDeadline = now.AddHours(4);
                }
                else
                {
                    booking.DepositDeadline = normalDeadline;
                }

                booking.UpdatedAt = now;

                await _context.SaveChangesAsync(cancellationToken);
                
                await _emailService.SendBookingApprovedAsync(booking);

                return new Response<string>("Đã duyệt đơn đặt phòng. Hệ thống đã gửi yêu cầu đặt cọc cho khách hàng.");
            }
            else if (request.Action == "confirm")
            {
                // Action này có thể dùng nếu KS không cần cọc và xác nhận thẳng
                booking.Status = BookingStatus.Confirmed;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await _emailService.SendBookingConfirmedAsync(booking);
                return new Response<string>("Đã xác nhận đơn đặt phòng.");
            }
            else if (request.Action == "reject")
            {
                if (string.IsNullOrWhiteSpace(request.CancelReason))
                    throw new ApiException("Vui lòng nhập lý do từ chối đơn đặt phòng.");

                booking.Status = BookingStatus.Cancelled;
                booking.CancelReason = request.CancelReason;
                booking.CancelledAt = DateTime.UtcNow;
                booking.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                await _emailService.SendBookingCancelledAsync(booking);
                return new Response<string>("Đã từ chối đơn đặt phòng.");
            }

            throw new ApiException("Hành động không hợp lệ. Chỉ chấp nhận 'approve', 'confirm' hoặc 'reject'.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────────
    // MockPaymentCommand: Giả lập thanh toán cọc (dùng trong Development/Testing)
    // ─────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Giả lập thanh toán cọ — chỉ dùng trong môi trường Dev/Test.
    /// Khi thanh toán thành công, tự động Confirm đơn nếu đang Pending.
    /// </summary>
    public class MockPaymentCommand : IRequest<Response<string>>
    {
        public Guid BookingId { get; set; }
        public Guid CustomerId { get; set; }  // Xác nhận quyền sở hữu
    }

    public class MockPaymentCommandHandler : IRequestHandler<MockPaymentCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IEmailService _emailService;
        public MockPaymentCommandHandler(IApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<Response<string>> Handle(MockPaymentCommand request, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.Hotel)
                .Include(b => b.Items).ThenInclude(i => i.RoomType)
                .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

            if (booking == null)
                throw new ApiException("Không tìm thấy đơn đặt phòng.");

            if (booking.CustomerId != request.CustomerId)
                throw new ApiException("Bạn không có quyền thanh toán cho đơn này.");

            if (booking.Status == BookingStatus.Cancelled)
                throw new ApiException("Đơn đặt phòng đã bị hủy, không thể thanh toán.");

            if (booking.Status != BookingStatus.Approved)
                throw new ApiException("Chỉ có thể thanh toán cọc khi đơn đặt phòng đã được quản lý phê duyệt (Approved).");

            if (booking.PaymentStatus == PaymentStatus.Paid)
                throw new ApiException("Đơn này đã được thanh toán cọ rồi.");

            var now = DateTime.UtcNow;

            // Cập nhật trạng thái thanh toán
            booking.PaymentStatus = PaymentStatus.Paid;
            booking.PaidAt = now;
            booking.UpdatedAt = now;

            // Tự động Confirm đơn sau khi cọc
            booking.Status = BookingStatus.Confirmed;

            await _context.SaveChangesAsync(cancellationToken);

            await _emailService.SendDepositConfirmedAsync(booking);

            return new Response<string>("Đã xác nhận thanh toán cọ thành công! Đơn đặt phòng của bạn đã được xác nhận.");
        }
    }
}
