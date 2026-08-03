using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Queries
{
    // ──────────────────────── DTOs ────────────────────────

    public class AdminBookingItem
    {
        public Guid Id { get; set; }
        public string GuestName { get; set; } = null!;
        public string GuestPhone { get; set; } = null!;
        public string GuestEmail { get; set; } = null!;
        public string HotelName { get; set; } = null!;
        public string BusinessName { get; set; } = null!;
        public string RoomTypeName { get; set; } = null!;
        public string CheckInDate { get; set; } = null!;   // "yyyy-MM-dd"
        public string CheckOutDate { get; set; } = null!;  // "yyyy-MM-dd"
        public int NumRooms { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public string Status { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public string? CancelReason { get; set; }
        public string CreatedAt { get; set; } = null!;     // ISO UTC string
    }

    public class AdminTopHotel
    {
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = null!;
        public string BusinessName { get; set; } = null!;
        public int TotalBookings { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminTopBusiness
    {
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = null!;
        public int TotalBookings { get; set; }
        public decimal Revenue { get; set; }
    }

    public class AdminHotelBookingSummary
    {
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = null!;
        public Guid BusinessId { get; set; }
        public string BusinessName { get; set; } = null!;
        public int TotalBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }   // chỉ Completed
        public decimal TotalDeposit { get; set; }   // PaymentStatus = Paid
        public string Title { get; set; } = null!;
    }

    public class AdminBookingStatsDto
    {
        // Tổng quan
        public int TotalBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalDeposit { get; set; }
        public int TotalCancelled { get; set; }

        // Top 5
        public List<AdminTopHotel> TopHotels { get; set; } = new();
        public List<AdminTopBusiness> TopBusinesses { get; set; } = new();

        // Bảng tổng hợp theo khách sạn
        public List<AdminHotelBookingSummary> HotelSummaries { get; set; } = new();

        // Danh sách booking phân trang
        public List<AdminBookingItem> Bookings { get; set; } = new();
        public int BookingTotalCount { get; set; }
    }

    // ──────────────────────── Query ────────────────────────

    public class GetAdminBookingStatsQuery : IRequest<Response<AdminBookingStatsDto>>
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public Guid? HotelId { get; set; }
        public Guid? BusinessId { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    // ──────────────────────── Handler ────────────────────────

    public class GetAdminBookingStatsQueryHandler
        : IRequestHandler<GetAdminBookingStatsQuery, Response<AdminBookingStatsDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetAdminBookingStatsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<AdminBookingStatsDto>> Handle(
            GetAdminBookingStatsQuery request,
            CancellationToken cancellationToken)
        {
            // ── 1. Xây dựng base query với filter ──

            // Đảm bảo tất cả DateTime đều là UTC khi truyền vào PostgreSQL
            DateTime? fromUtc = request.FromDate.HasValue
                ? DateTime.SpecifyKind(request.FromDate.Value, DateTimeKind.Utc)
                : null;

            DateTime? toUtc = request.ToDate.HasValue
                ? DateTime.SpecifyKind(request.ToDate.Value, DateTimeKind.Utc)
                : null;

            var query = _context.Bookings.AsQueryable();

            if (fromUtc.HasValue)
                query = query.Where(b => b.CreatedAt >= fromUtc.Value);

            if (toUtc.HasValue)
                query = query.Where(b => b.CreatedAt <= toUtc.Value);

            if (request.HotelId.HasValue)
                query = query.Where(b => b.HotelId == request.HotelId.Value);

            if (request.BusinessId.HasValue)
            {
                // Lấy danh sách HotelId thuộc business này trước (tránh subquery phức tạp trên EF)
                var hotelIds = await _context.Hotels
                    .Where(h => h.BusinessId == request.BusinessId.Value)
                    .Select(h => h.Id)
                    .ToListAsync(cancellationToken);

                query = query.Where(b => hotelIds.Contains(b.HotelId));
            }

            if (!string.IsNullOrEmpty(request.Status) &&
                Enum.TryParse<BookingStatus>(request.Status, out var parsedStatus))
            {
                query = query.Where(b => b.Status == parsedStatus);
            }

            // ── 2. Tổng quan (chạy trên filtered query) ──

            var totalBookings = await query.CountAsync(cancellationToken);
            var totalCancelled = await query.CountAsync(b => b.Status == BookingStatus.Cancelled, cancellationToken);

            var totalRevenue = await query
                .Where(b => b.Status == BookingStatus.Completed)
                .SumAsync(b => (decimal?)b.TotalPrice, cancellationToken) ?? 0m;

            var totalDeposit = await query
                .Where(b => b.PaymentStatus == PaymentStatus.Paid)
                .SumAsync(b => (decimal?)b.DepositAmount, cancellationToken) ?? 0m;

            // ── 3. Top 5 khách sạn (dựa trên filtered query, chỉ Completed) ──

            var topHotelRaw = await query
                .Where(b => b.Status == BookingStatus.Completed)
                .GroupBy(b => b.HotelId)
                .Select(g => new
                {
                    HotelId = g.Key,
                    Revenue = g.Sum(b => b.TotalPrice),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync(cancellationToken);

            var topHotelIds = topHotelRaw.Select(x => x.HotelId).ToList();
            var topHotelDetails = await _context.Hotels
                .Include(h => h.Business)
                .Where(h => topHotelIds.Contains(h.Id))
                .ToListAsync(cancellationToken);

            var topHotels = topHotelRaw
                .Select(x =>
                {
                    var hotel = topHotelDetails.First(h => h.Id == x.HotelId);
                    return new AdminTopHotel
                    {
                        HotelId = x.HotelId,
                        HotelName = hotel.Name,
                        BusinessName = hotel.Business.BusinessName,
                        TotalBookings = x.Count,
                        Revenue = x.Revenue
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // ── 4. Top 5 doanh nghiệp (tổng tất cả khách sạn thuộc doanh nghiệp) ──

            // Join booking -> hotel để lấy BusinessId
            var topBizRaw = await query
                .Where(b => b.Status == BookingStatus.Completed)
                .Join(_context.Hotels,
                    b => b.HotelId,
                    h => h.Id,
                    (b, h) => new { h.BusinessId, b.TotalPrice })
                .GroupBy(x => x.BusinessId)
                .Select(g => new
                {
                    BusinessId = g.Key,
                    Revenue = g.Sum(x => x.TotalPrice),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(5)
                .ToListAsync(cancellationToken);

            var topBizIds = topBizRaw.Select(x => x.BusinessId).ToList();
            var topBizDetails = await _context.Businesses
                .Where(b => topBizIds.Contains(b.Id))
                .ToListAsync(cancellationToken);

            var topBusinesses = topBizRaw
                .Select(x =>
                {
                    var biz = topBizDetails.First(b => b.Id == x.BusinessId);
                    return new AdminTopBusiness
                    {
                        BusinessId = x.BusinessId,
                        BusinessName = biz.BusinessName,
                        TotalBookings = x.Count,
                        Revenue = x.Revenue
                    };
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();

            // ── 5. Bảng tổng hợp theo khách sạn (tất cả KS trong filtered result) ──

            var hotelSummaryRaw = await query
                .GroupBy(b => b.HotelId)
                .Select(g => new
                {
                    HotelId = g.Key,
                    Total = g.Count(),
                    Completed = g.Count(b => b.Status == BookingStatus.Completed),
                    Cancelled = g.Count(b => b.Status == BookingStatus.Cancelled),
                    Revenue = g.Where(b => b.Status == BookingStatus.Completed)
                                .Sum(b => (decimal?)b.TotalPrice) ?? 0m,
                    Deposit = g.Where(b => b.PaymentStatus == PaymentStatus.Paid)
                                .Sum(b => (decimal?)b.DepositAmount) ?? 0m,
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync(cancellationToken);

            var summaryHotelIds = hotelSummaryRaw.Select(x => x.HotelId).ToList();
            var summaryHotels = await _context.Hotels
                .Include(h => h.Business)
                .Where(h => summaryHotelIds.Contains(h.Id))
                .ToListAsync(cancellationToken);

            var hotelSummaries = hotelSummaryRaw
                .Select(x =>
                {
                    var hotel = summaryHotels.FirstOrDefault(h => h.Id == x.HotelId);
                    return new AdminHotelBookingSummary
                    {
                        HotelId = x.HotelId,
                        HotelName = hotel?.Name ?? "—",
                        BusinessId = hotel?.BusinessId ?? Guid.Empty,
                        BusinessName = hotel?.Business?.BusinessName ?? "—",
                        TotalBookings = x.Total,
                        CompletedBookings = x.Completed,
                        CancelledBookings = x.Cancelled,
                        TotalRevenue = x.Revenue,
                        TotalDeposit = x.Deposit,
                        Title = "tieu de"
                    };
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ToList();

            // ── 6. Danh sách booking phân trang (chi tiết) ──

            var bookingTotalCount = totalBookings; // đã count ở trên
            var skip = (request.Page - 1) * request.PageSize;

            var bookingsRaw = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip(skip)
                .Take(request.PageSize)
                .Include(b => b.RoomType)
                .Include(b => b.Hotel)
                    .ThenInclude(h => h.Business)
                .ToListAsync(cancellationToken);

            var bookingItems = bookingsRaw.Select(b => new AdminBookingItem
            {
                Id = b.Id,
                GuestName = b.GuestName,
                GuestPhone = b.GuestPhone,
                GuestEmail = b.GuestEmail,
                HotelName = b.Hotel.Name,
                BusinessName = b.Hotel.Business.BusinessName,
                RoomTypeName = b.RoomType.Name,
                CheckInDate = b.CheckInDate.ToString("yyyy-MM-dd"),
                CheckOutDate = b.CheckOutDate.ToString("yyyy-MM-dd"),
                NumRooms = b.NumRooms,
                TotalPrice = b.TotalPrice,
                DepositAmount = b.DepositAmount,
                Status = b.Status.ToString(),
                PaymentStatus = b.PaymentStatus.ToString(),
                CancelReason = b.CancelReason,
                CreatedAt = b.CreatedAt.ToString("o") // ISO 8601 UTC
            }).ToList();

            // ── 7. Trả kết quả ──

            var result = new AdminBookingStatsDto
            {
                TotalBookings = totalBookings,
                TotalRevenue = totalRevenue,
                TotalDeposit = totalDeposit,
                TotalCancelled = totalCancelled,
                TopHotels = topHotels,
                TopBusinesses = topBusinesses,
                HotelSummaries = hotelSummaries,
                Bookings = bookingItems,
                BookingTotalCount = bookingTotalCount
            };

            return new Response<AdminBookingStatsDto>(result, "Lấy thống kê đặt phòng thành công.");
        }
    }
}
