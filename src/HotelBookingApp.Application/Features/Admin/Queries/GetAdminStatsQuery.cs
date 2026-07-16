using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Queries
{
    public class AdminStatsDto
    {
        // Doanh nghiệp
        public int TotalBusinesses { get; set; }
        public int PendingBusinesses { get; set; }
        public int ApprovedBusinesses { get; set; }

        // Khách sạn
        public int TotalHotels { get; set; }
        public int PendingHotels { get; set; }
        public int ActiveHotels { get; set; }

        // Đặt phòng
        public int TotalBookings { get; set; }
        public int BookingsToday { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }

        // Tài khoản
        public int TotalUsers { get; set; }
        public int TotalCustomers { get; set; }
    }

    public class GetAdminStatsQuery : IRequest<Response<AdminStatsDto>> { }

    public class GetAdminStatsQueryHandler : IRequestHandler<GetAdminStatsQuery, Response<AdminStatsDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetAdminStatsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<AdminStatsDto>> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var stats = new AdminStatsDto
            {
                // Doanh nghiệp
                TotalBusinesses    = await _context.Businesses.CountAsync(cancellationToken),
                PendingBusinesses  = await _context.Businesses.CountAsync(b => b.VerificationStatus == BusinessVerificationStatus.Pending, cancellationToken),
                ApprovedBusinesses = await _context.Businesses.CountAsync(b => b.VerificationStatus == BusinessVerificationStatus.Approved, cancellationToken),

                // Khách sạn
                TotalHotels   = await _context.Hotels.CountAsync(cancellationToken),
                PendingHotels = await _context.Hotels.CountAsync(h => h.ApprovalStatus == HotelApprovalStatus.Pending, cancellationToken),
                ActiveHotels  = await _context.Hotels.CountAsync(h => h.IsActive, cancellationToken),

                // Đặt phòng
                TotalBookings     = await _context.Bookings.CountAsync(cancellationToken),
                BookingsToday     = await _context.Bookings.CountAsync(b => b.CreatedAt.Date == DateTime.UtcNow.Date, cancellationToken),
                PendingBookings   = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending, cancellationToken),
                ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed, cancellationToken),

                // Tài khoản
                TotalUsers     = await _context.Users.CountAsync(cancellationToken),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role != null && u.Role.Name.ToLower() == "customer", cancellationToken),
            };

            return new Response<AdminStatsDto>(stats, "Lấy thống kê thành công.");
        }
    }
}
