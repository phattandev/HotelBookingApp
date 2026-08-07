using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Queries
{
    public class ManagerDailyCount
    {
        public string Date { get; set; } = null!;
        public int Count { get; set; }
    }

    public class ManagerMonthlyRevenue
    {
        public string Month { get; set; } = null!;
        public decimal Revenue { get; set; }
    }

    public class ManagerStatsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalDepositCollected { get; set; }

        public List<ManagerDailyCount> BookingsTrend { get; set; } = new();
        public List<ManagerMonthlyRevenue> RevenueTrend { get; set; } = new();
        public Dictionary<string, int> BookingStatusBreakdown { get; set; } = new();

        public double OccupancyToday { get; set; }
        public int CheckInsToday { get; set; }
        public int CheckOutsToday { get; set; }
        public int PendingBookings { get; set; }
        public int TotalRooms { get; set; }
    }

    public class GetManagerStatsQuery : IRequest<Response<ManagerStatsDto>>
    {
        public Guid ManagerId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class GetManagerStatsQueryHandler : IRequestHandler<GetManagerStatsQuery, Response<ManagerStatsDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetManagerStatsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<ManagerStatsDto>> Handle(GetManagerStatsQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            
            if (assignment == null)
            {
                return new Response<ManagerStatsDto>("Tài khoản không được phân công quản lý khách sạn nào.");
            }

            var query = _context.Bookings.Where(b => b.HotelId == assignment.HotelId).AsQueryable();

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
                : DateTime.UtcNow.AddDays(-7);
            var trendToDate = request.ToDate.HasValue 
                ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc) 
                : DateTime.UtcNow;

            var allBookingsInRange = await query.Where(b => b.CreatedAt >= trendDate && b.CreatedAt <= trendToDate)
                .Select(b => new { b.CreatedAt })
                .ToListAsync(cancellationToken);
            
            var grouped = allBookingsInRange.GroupBy(b => b.CreatedAt.Date)
                .ToDictionary(g => g.Key, g => g.Count());

            var bookingsTrend = new List<ManagerDailyCount>();
            for (var d = trendDate.Date; d <= trendToDate.Date; d = d.AddDays(1))
            {
                bookingsTrend.Add(new ManagerDailyCount
                {
                    Date = d.ToString("yyyy-MM-dd"),
                    Count = grouped.GetValueOrDefault(d, 0)
                });
            }

            // Monthly Revenue (last 6 months relative to today)
            var sixMonthsAgo = DateTime.UtcNow.AddMonths(-5);
            var monthlyRevenueQuery = await _context.Bookings
                .Where(b => b.HotelId == assignment.HotelId && b.Status == BookingStatus.Completed && b.CreatedAt >= DateTime.SpecifyKind(new DateTime(sixMonthsAgo.Year, sixMonthsAgo.Month, 1), DateTimeKind.Utc))
                .Select(b => new { b.CreatedAt, b.TotalPrice })
                .ToListAsync(cancellationToken);
            
            var groupedRevenue = monthlyRevenueQuery
                .GroupBy(b => new { b.CreatedAt.Year, b.CreatedAt.Month })
                .ToDictionary(g => $"{g.Key.Month:D2}/{g.Key.Year}", g => g.Sum(b => b.TotalPrice));

            var revenueTrend = new List<ManagerMonthlyRevenue>();
            for (int i = 5; i >= 0; i--)
            {
                var m = DateTime.UtcNow.AddMonths(-i);
                var key = $"{m.Month:D2}/{m.Year}";
                revenueTrend.Add(new ManagerMonthlyRevenue
                {
                    Month = key,
                    Revenue = groupedRevenue.GetValueOrDefault(key, 0)
                });
            }

            var todayDate = DateOnly.FromDateTime(DateTime.UtcNow);
            var todayBookings = await _context.Bookings
                .Include(b => b.Items)
                .Where(b => b.HotelId == assignment.HotelId && 
                            b.CheckInDate <= todayDate && 
                            b.CheckOutDate > todayDate && 
                            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Approved))
                .ToListAsync(cancellationToken);
            
            var roomsOccupied = todayBookings.Sum(b => b.Items.Sum(i => i.NumRooms));
            var totalRooms = await _context.RoomTypes
                .Where(rt => rt.HotelId == assignment.HotelId && rt.IsActive)
                .SumAsync(rt => rt.TotalRooms, cancellationToken);
            
            var occupancy = totalRooms > 0 ? (double)roomsOccupied / totalRooms : 0;

            var checkInsToday = await _context.Bookings
                .Where(b => b.HotelId == assignment.HotelId && 
                            b.CheckInDate == todayDate && 
                            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Approved))
                .CountAsync(cancellationToken);
                
            var checkOutsToday = await _context.Bookings
                .Where(b => b.HotelId == assignment.HotelId && 
                            b.CheckOutDate == todayDate && 
                            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Approved))
                .CountAsync(cancellationToken);

            var pendingBookings = await _context.Bookings
                .Where(b => b.HotelId == assignment.HotelId && b.Status == BookingStatus.Pending)
                .CountAsync(cancellationToken);

            var stats = new ManagerStatsDto
            {
                TotalRevenue = totalRevenue,
                TotalDepositCollected = totalDeposit,
                BookingStatusBreakdown = statusBreakdown,
                BookingsTrend = bookingsTrend,
                RevenueTrend = revenueTrend,
                OccupancyToday = occupancy,
                CheckInsToday = checkInsToday,
                CheckOutsToday = checkOutsToday,
                PendingBookings = pendingBookings,
                TotalRooms = totalRooms
            };

            return new Response<ManagerStatsDto>(stats, "Lấy thống kê quản lý thành công.");
        }
    }
}
