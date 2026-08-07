using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.BusinessStaff.Queries
{
    public class BusinessDailyCount
    {
        public string Date { get; set; } = null!;
        public int Count { get; set; }
    }

    public class BusinessRecentBooking
    {
        public Guid Id { get; set; }
        public string GuestName { get; set; } = null!;
        public string HotelName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = null!;
        public decimal TotalPrice { get; set; }
    }

    public class BusinessStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalDepositCollected { get; set; }

        public List<BusinessDailyCount> BookingsTrend { get; set; } = new();
        public Dictionary<string, int> BookingStatusBreakdown { get; set; } = new();

        public int TotalHotels { get; set; }
        public int ActiveHotels { get; set; }
        public int PendingHotels { get; set; }
        public int TotalStaffAssigned { get; set; }

        public List<BusinessRecentBooking> RecentBookings { get; set; } = new();
    }

    public class GetBusinessStatsQuery : IRequest<Response<BusinessStatsDto>>
    {
        public Guid UserId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class GetBusinessStatsQueryHandler : IRequestHandler<GetBusinessStatsQuery, Response<BusinessStatsDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetBusinessStatsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<BusinessStatsDto>> Handle(GetBusinessStatsQuery request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.OwnerId == request.UserId, cancellationToken);
            
            if (business == null)
            {
                return new Response<BusinessStatsDto>("Tài khoản không phải là đối tác hoặc chưa đăng ký doanh nghiệp.");
            }

            var hotelIds = await _context.Hotels
                .Where(h => h.BusinessId == business.Id)
                .Select(h => h.Id)
                .ToListAsync(cancellationToken);

            var query = _context.Bookings.Where(b => hotelIds.Contains(b.HotelId)).AsQueryable();

            if (request.FromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc);
                query = query.Where(b => b.CreatedAt >= fromUtc);
            }
            if (request.ToDate.HasValue)
            {
                var toUtc = DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc);
                query = query.Where(b => b.CreatedAt <= toUtc);
            }

            var completedBookings = await query.Where(b => b.Status == BookingStatus.Completed).ToListAsync(cancellationToken);
            var totalRevenue = completedBookings.Sum(b => b.TotalPrice);

            var paidBookings = await query.Where(b => b.PaymentStatus == PaymentStatus.Paid).ToListAsync(cancellationToken);
            var totalDeposit = paidBookings.Sum(b => b.DepositAmount);

            var statuses = await query.GroupBy(b => b.Status)
                                      .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                      .ToListAsync(cancellationToken);
            var statusBreakdown = statuses.ToDictionary(x => x.Status, x => x.Count);

            var trendDate = request.FromDate.HasValue 
                ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow.AddDays(-30);
            var trendToDate = request.ToDate.HasValue 
                ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow;

            var allBookingsInRange = await query.Where(b => b.CreatedAt >= trendDate && b.CreatedAt <= trendToDate)
                .Select(b => new { b.CreatedAt })
                .ToListAsync(cancellationToken);
            
            var grouped = allBookingsInRange.GroupBy(b => b.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var bookingsTrend = new List<BusinessDailyCount>();
            for (var d = trendDate.Date; d <= trendToDate.Date; d = d.AddDays(1))
            {
                bookingsTrend.Add(new BusinessDailyCount
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Count = grouped.GetValueOrDefault(d, 0)
                });
            }

            var recentBookings = await query
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new BusinessRecentBooking
                {
                    Id = b.Id,
                    GuestName = b.GuestName,
                    HotelName = b.Hotel.Name,
                    CreatedAt = b.CreatedAt,
                    Status = b.Status.ToString(),
                    TotalPrice = b.TotalPrice
                })
                .ToListAsync(cancellationToken);

            var totalStaffAssigned = await _context.HotelStaffAssignments
                .Where(a => hotelIds.Contains(a.HotelId))
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync(cancellationToken);

            var stats = new BusinessStatsDto
            {
                TotalRevenue = totalRevenue,
                TotalDepositCollected = totalDeposit,
                BookingStatusBreakdown = statusBreakdown,
                BookingsTrend = bookingsTrend,
                RecentBookings = recentBookings,
                TotalHotels = hotelIds.Count,
                ActiveHotels = await _context.Hotels.CountAsync(h => h.BusinessId == business.Id && h.IsActive, cancellationToken),
                PendingHotels = await _context.Hotels.CountAsync(h => h.BusinessId == business.Id && h.ApprovalStatus == HotelApprovalStatus.Pending, cancellationToken),
                TotalStaffAssigned = totalStaffAssigned
            };

            return new Response<BusinessStatsDto>(stats, "Lấy thống kê doanh nghiệp thành công.");
        }
    }
}
