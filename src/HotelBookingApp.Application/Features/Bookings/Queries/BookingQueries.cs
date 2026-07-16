using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Application.DTOs.BookingDto;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Bookings.Queries
{
    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Query lấy danh sách đơn đặt phòng của khách hàng đang đăng nhập.
    /// </summary>
    public class GetMyBookingsQuery : IRequest<Response<List<BookingDto>>>
    {
        public Guid CustomerId { get; set; }
        /// <summary>Lọc theo trạng thái (optional). Ví dụ: "Pending", "Confirmed", v.v.</summary>
        public string? StatusFilter { get; set; }
    }

    public class GetMyBookingsQueryHandler : IRequestHandler<GetMyBookingsQuery, Response<List<BookingDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetMyBookingsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<BookingDto>>> Handle(GetMyBookingsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Bookings
                .Include(b => b.RoomType)
                    .ThenInclude(rt => rt.Hotel)
                .Include(b => b.RoomType.Images)
                .Where(b => b.CustomerId == request.CustomerId)
                .AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
                Enum.TryParse<BookingStatus>(request.StatusFilter, true, out var statusEnum))
            {
                query = query.Where(b => b.Status == statusEnum);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            // Lấy danh sách BookingId đã được đánh giá
            var bookingIds = bookings.Select(b => b.Id).ToList();
            var reviewedBookingIds = await _context.Reviews
                .Where(r => bookingIds.Contains(r.BookingId))
                .Select(r => r.BookingId)
                .ToListAsync(cancellationToken);
            var reviewedSet = reviewedBookingIds.ToHashSet();

            var result = bookings.Select(b => BookingMapper.MapToDto(b, reviewedSet.Contains(b.Id))).ToList();
            return new Response<List<BookingDto>>(result, $"Bạn có {result.Count} đơn đặt phòng.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Query lấy chi tiết 1 đơn đặt phòng (dùng cho trang BookingConfirmation).
    /// </summary>
    public class GetBookingDetailQuery : IRequest<Response<BookingDto>>
    {
        public Guid BookingId { get; set; }
        public Guid CustomerId { get; set; }    // Để xác minh chỉ chủ đơn mới xem được
    }

    public class GetBookingDetailQueryHandler : IRequestHandler<GetBookingDetailQuery, Response<BookingDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetBookingDetailQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<BookingDto>> Handle(GetBookingDetailQuery request, CancellationToken cancellationToken)
        {
            var booking = await _context.Bookings
                .Include(b => b.RoomType)
                    .ThenInclude(rt => rt.Hotel)
                .Include(b => b.RoomType.Images)
                .FirstOrDefaultAsync(b =>
                    b.Id == request.BookingId &&
                    b.CustomerId == request.CustomerId,
                    cancellationToken);

            if (booking == null)
                throw new ApiException("Không tìm thấy đơn đặt phòng.");

            return new Response<BookingDto>(BookingMapper.MapToDto(booking), "Lấy chi tiết đơn đặt phòng thành công.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Query cho Manager lấy danh sách đơn đặt phòng của khách sạn đang quản lý.
    /// </summary>
    public class GetHotelBookingsQuery : IRequest<Response<List<BookingDto>>>
    {
        public Guid ManagerId { get; set; }
        public string? StatusFilter { get; set; }
    }

    public class GetHotelBookingsQueryHandler : IRequestHandler<GetHotelBookingsQuery, Response<List<BookingDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetHotelBookingsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<BookingDto>>> Handle(GetHotelBookingsQuery request, CancellationToken cancellationToken)
        {
            // Xác định khách sạn Manager đang quản lý
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null)
                throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            // Lấy tất cả đơn thuộc các phòng của khách sạn này
            var query = _context.Bookings
                .Include(b => b.RoomType)
                    .ThenInclude(rt => rt.Hotel)
                .Include(b => b.RoomType.Images)
                .Where(b => b.RoomType.HotelId == assignment.HotelId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.StatusFilter) &&
                Enum.TryParse<BookingStatus>(request.StatusFilter, true, out var statusEnum))
            {
                query = query.Where(b => b.Status == statusEnum);
            }

            var bookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync(cancellationToken);

            var result = bookings.Select(b => BookingMapper.MapToDto(b)).ToList();
            return new Response<List<BookingDto>>(result, $"Tìm thấy {result.Count} đơn đặt phòng.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Hàm dùng chung để ánh xạ Booking entity → BookingDto
    // ─────────────────────────────────────────────────────────────────────────────
    internal static class BookingMapper
    {
        internal static BookingDto MapToDto(Booking b, bool hasReview = false)
        {
            var primaryImg = b.RoomType?.Images?.FirstOrDefault(i => i.IsPrimary) ?? b.RoomType?.Images?.FirstOrDefault();

            // Nếu đơn đã duyệt nhưng DepositDeadline chưa được set (đơn cũ trước khi update logic),
            // tự tính lại deadline: 24h trước check-in, hoặc 4h ân hạn nếu đã qua mốc đó.
            DateTime? resolvedDeadline = b.DepositDeadline;
            if (resolvedDeadline == null
                && b.DepositAmount > 0
                && (b.Status == BookingStatus.Approved || b.Status == BookingStatus.Confirmed)
                && b.PaymentStatus != PaymentStatus.Paid)
            {
                var checkInMidnight = b.CheckInDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                var normalDeadline = checkInMidnight.AddHours(-24);
                resolvedDeadline = normalDeadline > DateTime.UtcNow ? normalDeadline : DateTime.UtcNow.AddHours(4);
            }

            return new BookingDto
            {
                Id = b.Id,
                Status = b.Status.ToString(),
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                NumRooms = b.NumRooms,
                TotalPrice = b.TotalPrice,
                GuestName = b.GuestName,
                GuestPhone = b.GuestPhone,
                GuestEmail = b.GuestEmail,
                SpecialRequests = b.SpecialRequests,
                CancelReason = b.CancelReason,
                CreatedAt = b.CreatedAt,
                RoomTypeId = b.RoomTypeId,
                RoomTypeName = b.RoomType?.Name ?? string.Empty,
                HotelName = b.RoomType?.Hotel?.Name ?? string.Empty,
                HotelId = b.RoomType?.HotelId ?? Guid.Empty,
                HotelAddress = b.RoomType?.Hotel?.AddressLine ?? string.Empty,
                RoomImageUrl = primaryImg?.Url,
                // Thanh toán cọc
                PaymentStatus = b.PaymentStatus.ToString(),
                DepositAmount = b.DepositAmount,
                DepositDeadline = resolvedDeadline,
                PaidAt = b.PaidAt,
                RefundAmount = b.RefundAmount,
                // Đánh giá
                HasReview = hasReview,
            };
        }
    }
}
