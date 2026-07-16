using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    /// <summary>
    /// DTO trả về thông tin chi tiết phòng, bao gồm số phòng còn trống trong khoảng ngày đặt.
    /// </summary>
    public class RoomTypePublicDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        public int TotalRooms { get; set; }
        public string Description { get; set; } = string.Empty;
        /// <summary>Số phòng còn trống. Null nếu không truyền checkIn/checkOut.</summary>
        public int? AvailableRooms { get; set; }
        public List<HotelImageDto> Images { get; set; } = new();
        public List<AmenityDto> Amenities { get; set; } = new();
    }

    /// <summary>
    /// DTO trả về thông tin đầy đủ của 1 khách sạn cho trang chi tiết public.
    /// </summary>
    public class HotelPublicDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string ProvinceName { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;
        public int? StarRating { get; set; }
        public string? Description { get; set; }
        public List<HotelImageDto> Images { get; set; } = new();
        public List<AmenityDto> Amenities { get; set; } = new();
        public List<RoomTypePublicDto> RoomTypes { get; set; } = new();
    }

    /// <summary>
    /// Query lấy chi tiết khách sạn cho trang public (không cần đăng nhập).
    /// Nếu truyền checkIn + checkOut, sẽ tính số phòng còn trống cho mỗi loại phòng.
    /// </summary>
    public class GetHotelPublicDetailQuery : IRequest<Response<HotelPublicDetailDto>>
    {
        public Guid HotelId { get; set; }
        /// <summary>Ngày nhận phòng (optional). Dùng để tính phòng còn trống.</summary>
        public DateOnly? CheckIn { get; set; }
        /// <summary>Ngày trả phòng (optional).</summary>
        public DateOnly? CheckOut { get; set; }
    }

    public class GetHotelPublicDetailQueryHandler : IRequestHandler<GetHotelPublicDetailQuery, Response<HotelPublicDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetHotelPublicDetailQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<HotelPublicDetailDto>> Handle(GetHotelPublicDetailQuery request, CancellationToken cancellationToken)
        {
            // Lấy khách sạn kèm tất cả dữ liệu cần thiết cho trang chi tiết
            var hotel = await _context.Hotels
                .Include(h => h.Ward).ThenInclude(w => w.Province)
                .Include(h => h.Images)
                .Include(h => h.HotelAmenities)
                    .ThenInclude(ha => ha.Amenity)
                        .ThenInclude(a => a.Category)
                .Include(h => h.RoomTypes.Where(rt => rt.IsActive))
                    .ThenInclude(rt => rt.Images)
                .Include(h => h.RoomTypes.Where(rt => rt.IsActive))
                    .ThenInclude(rt => rt.RoomTypeAmenities)
                        .ThenInclude(ra => ra.Amenity)
                            .ThenInclude(a => a.Category)
                .FirstOrDefaultAsync(h =>
                    h.Id == request.HotelId &&
                    h.ApprovalStatus == HotelApprovalStatus.Approved &&
                    h.IsActive,
                    cancellationToken);

            if (hotel == null)
                throw new ApiException("Không tìm thấy khách sạn hoặc khách sạn hiện không hoạt động.");

            // Ánh xạ dữ liệu phòng — nếu có checkIn/checkOut thì tính phòng còn trống
            var roomTypeDtos = new List<RoomTypePublicDto>();
            foreach (var rt in hotel.RoomTypes)
            {
                int? availableRooms = null;

                if (request.CheckIn.HasValue && request.CheckOut.HasValue)
                {
                    // Đếm số phòng đã bị đặt (booking đang active và có ngày overlap với khoảng thời gian yêu cầu)
                    // Overlap xảy ra khi: booking.CheckIn < request.CheckOut VÀ booking.CheckOut > request.CheckIn
                    var bookedRooms = await _context.Bookings
                        .Where(b =>
                            b.RoomTypeId == rt.Id &&
                            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed) &&
                            b.CheckInDate < request.CheckOut.Value &&
                            b.CheckOutDate > request.CheckIn.Value)
                        .SumAsync(b => b.NumRooms, cancellationToken);

                    // Số phòng còn lại = tổng phòng - số đã bị đặt (không âm)
                    availableRooms = Math.Max(0, rt.TotalRooms - bookedRooms);
                }

                roomTypeDtos.Add(new RoomTypePublicDto
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    BasePrice = rt.BasePrice,
                    MaxAdults = rt.MaxAdults,
                    MaxChildren = rt.MaxChildren,
                    TotalRooms = rt.TotalRooms,
                    Description = rt.Description,
                    AvailableRooms = availableRooms,
                    Images = rt.Images.OrderBy(i => i.DisplayOrder)
                        .Select(i => new HotelImageDto { Id = i.Id, Url = i.Url, PublicId = i.PublicId, IsPrimary = i.IsPrimary, DisplayOrder = i.DisplayOrder })
                        .ToList(),
                    Amenities = rt.RoomTypeAmenities
                        .Select(ra => new AmenityDto { Id = ra.AmenityId, Name = ra.Amenity.Name, CategoryName = ra.Amenity.Category.Name, ApplicableTo = ra.Amenity.Category.ApplicableTo })
                        .ToList()
                });
            }

            var dto = new HotelPublicDetailDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                AddressLine = hotel.AddressLine,
                ProvinceName = hotel.Ward?.Province?.Name ?? string.Empty,
                WardName = hotel.Ward?.Name ?? string.Empty,
                StarRating = hotel.StarRating,
                Description = hotel.Description,
                Images = hotel.Images.OrderBy(i => i.DisplayOrder)
                    .Select(i => new HotelImageDto { Id = i.Id, Url = i.Url, PublicId = i.PublicId, IsPrimary = i.IsPrimary, DisplayOrder = i.DisplayOrder })
                    .ToList(),
                Amenities = hotel.HotelAmenities
                    .Select(ha => new AmenityDto { Id = ha.AmenityId, Name = ha.Amenity.Name, CategoryName = ha.Amenity.Category.Name, ApplicableTo = ha.Amenity.Category.ApplicableTo })
                    .ToList(),
                RoomTypes = roomTypeDtos
            };

            return new Response<HotelPublicDetailDto>(dto, "Lấy chi tiết khách sạn thành công.");
        }
    }
}
