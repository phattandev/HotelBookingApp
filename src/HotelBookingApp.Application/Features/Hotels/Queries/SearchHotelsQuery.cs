using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    /// <summary>
    /// Query tìm kiếm khách sạn cho trang public (không cần đăng nhập).
    /// Lọc theo tên/địa điểm, khoảng giá, tiện nghi khách sạn, tiện nghi phòng và tên loại phòng.
    /// Tất cả điều kiện kết hợp với nhau theo phép AND.
    /// </summary>
    public class SearchHotelsQuery : IRequest<PaginatedResponse<HotelSearchResultDto>>
    {
        /// <summary>Từ khóa tìm kiếm: khớp với tên khách sạn hoặc tên tỉnh/thành phố.</summary>
        public string? Q { get; set; }

        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }

        /// <summary>Giá tối thiểu (lọc theo BasePrice của RoomType thấp nhất).</summary>
        public decimal? MinPrice { get; set; }

        /// <summary>Giá tối đa.</summary>
        public decimal? MaxPrice { get; set; }

        /// <summary>
        /// Danh sách ID tiện nghi khách sạn (hotel-level amenities).
        /// Chỉ lấy KS có TẤT CẢ các tiện nghi được chỉ định (AND logic).
        /// </summary>
        public List<Guid>? HotelAmenityIds { get; set; }

        /// <summary>
        /// Danh sách ID tiện nghi phòng (room-level amenities).
        /// Chỉ lấy KS có ít nhất 1 loại phòng có TẤT CẢ các tiện nghi được chỉ định (AND logic).
        /// </summary>
        public List<Guid>? RoomAmenityIds { get; set; }

        /// <summary>
        /// Danh sách tên loại phòng (ví dụ: "Deluxe", "Suite").
        /// Chỉ lấy KS có ít nhất 1 loại phòng khớp tên (OR logic giữa các tên phòng).
        /// </summary>
        public List<string>? RoomTypeNames { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SearchHotelsQueryHandler : IRequestHandler<SearchHotelsQuery, PaginatedResponse<HotelSearchResultDto>>
    {
        private readonly IApplicationDbContext _context;
        public SearchHotelsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<PaginatedResponse<HotelSearchResultDto>> Handle(SearchHotelsQuery request, CancellationToken cancellationToken)
        {
            // Chỉ hiển thị các khách sạn đã được Admin duyệt và đang hoạt động
            var query = _context.Hotels
                .Where(h => h.ApprovalStatus == Domain.Models.HotelApprovalStatus.Approved && h.IsActive)
                .Include(h => h.Ward).ThenInclude(w => w.Province)
                .Include(h => h.Images)
                .Include(h => h.RoomTypes.Where(rt => rt.IsActive))
                    .ThenInclude(rt => rt.RoomTypeAmenities)
                .Include(h => h.HotelAmenities)
                .AsQueryable();

            // Lọc theo từ khóa: so sánh với tên KS hoặc tên tỉnh (không phân biệt hoa thường)
            if (!string.IsNullOrWhiteSpace(request.Q))
            {
                var keyword = request.Q.ToLower();
                query = query.Where(h =>
                    h.Name.ToLower().Contains(keyword) ||
                    (h.Ward != null && h.Ward.Province != null && h.Ward.Province.Name.ToLower().Contains(keyword)) ||
                    (h.Ward != null && h.Ward.Name.ToLower().Contains(keyword)) ||
                    h.AddressLine.ToLower().Contains(keyword)
                );
            }

            // Lọc theo giá tối thiểu: lấy KS có ít nhất 1 RoomType có giá >= MinPrice
            if (request.MinPrice.HasValue)
                query = query.Where(h => h.RoomTypes.Any(rt => rt.IsActive && rt.BasePrice >= request.MinPrice.Value));

            // Lọc theo giá tối đa: lấy KS có ít nhất 1 RoomType có giá <= MaxPrice
            if (request.MaxPrice.HasValue)
                query = query.Where(h => h.RoomTypes.Any(rt => rt.IsActive && rt.BasePrice <= request.MaxPrice.Value));

            // Lọc theo tiện nghi khách sạn (AND): KS phải có TẤT CẢ các amenity được chỉ định
            if (request.HotelAmenityIds != null && request.HotelAmenityIds.Count > 0)
            {
                foreach (var amenityId in request.HotelAmenityIds)
                {
                    var aid = amenityId; // closure safety
                    query = query.Where(h => h.HotelAmenities.Any(ha => ha.AmenityId == aid));
                }
            }

            // Gom tất cả các bộ lọc liên quan đến loại phòng thành một điều kiện duy nhất:
            // KS phải có ít nhất 1 loại phòng thoả mãn TẤT CẢ các tiêu chí (Active, Amenities, Availability, Names)
            var roomAmenityIds = request.RoomAmenityIds ?? new List<Guid>();
            var hasRoomAmenities = roomAmenityIds.Count > 0;
            
            var lowerNames = request.RoomTypeNames != null ? request.RoomTypeNames.Select(n => n.ToLower()).ToList() : new List<string>();
            var hasRoomNames = lowerNames.Count > 0;
            
            var hasDates = request.CheckIn.HasValue && request.CheckOut.HasValue;
            var checkIn = request.CheckIn ?? default;
            var checkOut = request.CheckOut ?? default;

            if (hasRoomAmenities || hasRoomNames || hasDates)
            {
                query = query.Where(h => h.RoomTypes.Any(rt => 
                    rt.IsActive &&
                    (!hasRoomNames || lowerNames.Contains(rt.Name.ToLower())) &&
                    (!hasRoomAmenities || roomAmenityIds.All(aid => rt.RoomTypeAmenities.Any(rta => rta.AmenityId == aid))) &&
                    (!hasDates || rt.TotalRooms > (_context.Bookings.Where(b => b.RoomTypeId == rt.Id &&
                            (b.Status == Domain.Models.BookingStatus.Pending || b.Status == Domain.Models.BookingStatus.Approved || b.Status == Domain.Models.BookingStatus.Confirmed) &&
                            b.CheckInDate < checkOut && b.CheckOutDate > checkIn)
                        .Sum(b => (int?)b.NumRooms) ?? 0))
                ));
            }

            var totalCount = await query.CountAsync(cancellationToken);
            var hotels = await query.OrderBy(h => h.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // Ánh xạ sang DTO, tính giá thấp nhất và lấy ảnh đại diện
            var result = hotels.Select(h =>
            {
                var activeRoomTypes = h.RoomTypes.Where(rt => rt.IsActive).ToList();
                var primaryImage = h.Images.FirstOrDefault(i => i.IsPrimary) ?? h.Images.FirstOrDefault();

                return new HotelSearchResultDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    AddressLine = h.AddressLine,
                    ProvinceName = h.Ward?.Province?.Name ?? string.Empty,
                    WardName = h.Ward?.Name ?? string.Empty,
                    StarRating = h.StarRating,
                    PrimaryImageUrl = primaryImage?.Url,
                    MinPrice = activeRoomTypes.Any() ? activeRoomTypes.Min(rt => rt.BasePrice) : null
                };
            }).ToList();

            return new PaginatedResponse<HotelSearchResultDto>(result, totalCount, request.Page, request.PageSize);
        }
    }

    public class SearchHotelsQueryValidator : FluentValidation.AbstractValidator<SearchHotelsQuery>
    {
        public SearchHotelsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Trang phải lớn hơn hoặc bằng 1.");
            RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).WithMessage("Số lượng kết quả trên trang phải lớn hơn hoặc bằng 1.");
            RuleFor(x => x.CheckOut)
                .GreaterThan(x => x.CheckIn).When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
                .WithMessage("Ngày trả phòng phải sau ngày nhận phòng.");
        }
    }
}
