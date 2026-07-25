using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Queries
{
    public class DailyCount
    {
        public string Date { get; set; } = null!;
        public int Count { get; set; }
    }

    public class HotelRevenueSummary
    {
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = null!;
        public decimal Revenue { get; set; }
    }

    public class AdminStatsDto
    {
        // Doanh thu
        public decimal TotalRevenue { get; set; }
        public decimal TotalDepositCollected { get; set; }

        // Biểu đồ và phân bổ
        public List<DailyCount> BookingsTrend { get; set; } = new();
        public Dictionary<string, int> BookingStatusBreakdown { get; set; } = new();
        public List<HotelRevenueSummary> TopHotelsByRevenue { get; set; } = new();

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

    public class GetAdminStatsQuery : IRequest<Response<AdminStatsDto>>
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class GetAdminStatsQueryHandler : IRequestHandler<GetAdminStatsQuery, Response<AdminStatsDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetAdminStatsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<AdminStatsDto>> Handle(GetAdminStatsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Bookings.AsQueryable();

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

            // Breakdown
            var statuses = await query.GroupBy(b => b.Status)
                                      .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                                      .ToListAsync(cancellationToken);
            var statusBreakdown = statuses.ToDictionary(x => x.Status, x => x.Count);

            // Trend
            var trendDate = request.FromDate.HasValue 
                ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow.AddDays(-7);
            var trendToDate = request.ToDate.HasValue 
                ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow;
            
            var allBookingsInRange = await query.Where(b => b.CreatedAt >= trendDate && b.CreatedAt <= trendToDate)
                .Select(b => new { b.CreatedAt })
                .ToListAsync(cancellationToken);
            
            var grouped = allBookingsInRange.GroupBy(b => b.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var bookingsTrend = new List<DailyCount>();
            for (var d = trendDate.Date; d <= trendToDate.Date; d = d.AddDays(1))
            {
                bookingsTrend.Add(new DailyCount
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Count = grouped.GetValueOrDefault(d, 0)
                });
            }

            // Top Hotels
            var topHotels = await query.Where(b => b.Status == BookingStatus.Completed)
                .GroupBy(b => b.HotelId)
                .Select(g => new { HotelId = g.Key, Revenue = g.Sum(b => b.TotalPrice) })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync(cancellationToken);

            var topHotelIds = topHotels.Select(h => h.HotelId).ToList();
            var hotelNames = await _context.Hotels.Where(h => topHotelIds.Contains(h.Id))
                .ToDictionaryAsync(h => h.Id, h => h.Name, cancellationToken);

            var topHotelsByRevenue = topHotels.Select(h => new HotelRevenueSummary
            {
                HotelId = h.HotelId,
                HotelName = hotelNames.GetValueOrDefault(h.HotelId, "Unknown"),
                Revenue = h.Revenue
            }).ToList();


            var stats = new AdminStatsDto
            {
                TotalRevenue = totalRevenue,
                TotalDepositCollected = totalDeposit,
                BookingStatusBreakdown = statusBreakdown,
                BookingsTrend = bookingsTrend,
                TopHotelsByRevenue = topHotelsByRevenue,

                TotalBusinesses    = await _context.Businesses.CountAsync(cancellationToken),
                PendingBusinesses  = await _context.Businesses.CountAsync(b => b.VerificationStatus == BusinessVerificationStatus.Pending, cancellationToken),
                ApprovedBusinesses = await _context.Businesses.CountAsync(b => b.VerificationStatus == BusinessVerificationStatus.Approved, cancellationToken),

                TotalHotels   = await _context.Hotels.CountAsync(cancellationToken),
                PendingHotels = await _context.Hotels.CountAsync(h => h.ApprovalStatus == HotelApprovalStatus.Pending, cancellationToken),
                ActiveHotels  = await _context.Hotels.CountAsync(h => h.IsActive, cancellationToken),

                TotalBookings     = await _context.Bookings.CountAsync(cancellationToken),
                BookingsToday     = await _context.Bookings.CountAsync(b => b.CreatedAt.Date == DateTime.UtcNow.Date, cancellationToken),
                PendingBookings   = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Pending, cancellationToken),
                ConfirmedBookings = await _context.Bookings.CountAsync(b => b.Status == BookingStatus.Confirmed, cancellationToken),

                TotalUsers     = await _context.Users.CountAsync(cancellationToken),
                TotalCustomers = await _context.Users.CountAsync(u => u.Role != null && u.Role.Name.ToLower() == "customer", cancellationToken),
            };

            return new Response<AdminStatsDto>(stats, "Lấy thống kê thành công.");
        }
    }
}
