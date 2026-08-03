using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    public class GetPartnerHotelDetailQuery : IRequest<Response<ManagedHotelDetailDto>>
    {
        public Guid PartnerId { get; set; }
        public Guid HotelId { get; set; }
        public GetPartnerHotelDetailQuery(Guid partnerId, Guid hotelId)
        {
            PartnerId = partnerId;
            HotelId = hotelId;
        }
    }

    public class GetPartnerHotelDetailQueryHandler : IRequestHandler<GetPartnerHotelDetailQuery, Response<ManagedHotelDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetPartnerHotelDetailQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<ManagedHotelDetailDto>> Handle(GetPartnerHotelDetailQuery request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Không tìm thấy hồ sơ doanh nghiệp.");

            var hotel = await _context.Hotels
                .Include(h => h.Ward)
                    .ThenInclude(w => w.Province)
                .Include(h => h.Images)
                .Include(h => h.HotelAmenities)
                    .ThenInclude(ha => ha.Amenity)
                        .ThenInclude(a => a.Category)
                .Include(h => h.RoomTypes)
                    .ThenInclude(rt => rt.Images)
                .Include(h => h.RoomTypes)
                    .ThenInclude(rt => rt.RoomTypeAmenities)
                        .ThenInclude(ra => ra.Amenity)
                            .ThenInclude(a => a.Category)
                .FirstOrDefaultAsync(h => h.Id == request.HotelId && h.BusinessId == business.Id, cancellationToken);

            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn hoặc bạn không có quyền xem.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var activeBookings = await _context.Bookings
                .Where(b => b.HotelId == hotel.Id &&
                            (b.Status == Domain.Models.BookingStatus.Pending || b.Status == Domain.Models.BookingStatus.Approved || b.Status == Domain.Models.BookingStatus.Confirmed) &&
                            b.CheckInDate <= today &&
                            b.CheckOutDate > today)
                .ToListAsync(cancellationToken);

            var dto = new ManagedHotelDetailDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
                ProvinceName = hotel.Ward?.Province?.Name ?? string.Empty,
                ProvinceId = hotel.Ward?.ProvinceId,
                WardName = hotel.Ward?.Name ?? string.Empty,
                WardId = hotel.WardId,
                AddressLine = hotel.AddressLine,
                Description = hotel.Description,
                StarRating = hotel.StarRating,
                ApprovalStatus = hotel.ApprovalStatus.ToString(),
                IsActive = hotel.IsActive,
                Images = hotel.Images
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new HotelImageDto
                    {
                        Id = i.Id,
                        Url = i.Url,
                        PublicId = i.PublicId,
                        IsPrimary = i.IsPrimary,
                        DisplayOrder = i.DisplayOrder
                    }).ToList(),
                Amenities = hotel.HotelAmenities
                    .Select(ha => new AmenityDto
                    {
                        Id = ha.AmenityId,
                        Name = ha.Amenity.Name,
                        CategoryName = ha.Amenity.Category.Name,
                        ApplicableTo = ha.Amenity.Category.ApplicableTo
                    }).ToList(),
                RoomTypes = hotel.RoomTypes
                    .Select(rt => {
                        var bookedRooms = activeBookings.Where(b => b.RoomTypeId == rt.Id).Sum(b => b.NumRooms);
                        return new RoomTypeDto
                        {
                            Id = rt.Id,
                            Name = rt.Name,
                            BasePrice = rt.BasePrice,
                            MaxAdults = rt.MaxAdults,
                            MaxChildren = rt.MaxChildren,
                            TotalRooms = rt.TotalRooms,
                            BookedRooms = bookedRooms,
                            AvailableRooms = Math.Max(0, rt.TotalRooms - bookedRooms),
                            Description = rt.Description,
                            IsActive = rt.IsActive,
                            Images = rt.Images
                                .OrderBy(i => i.DisplayOrder)
                                .Select(i => new HotelImageDto
                                {
                                    Id = i.Id,
                                    Url = i.Url,
                                    PublicId = i.PublicId,
                                    IsPrimary = i.IsPrimary,
                                    DisplayOrder = i.DisplayOrder
                                }).ToList(),
                            Amenities = rt.RoomTypeAmenities
                                .Select(ra => new AmenityDto
                                {
                                    Id = ra.AmenityId,
                                    Name = ra.Amenity.Name,
                                    CategoryName = ra.Amenity.Category.Name,
                                    ApplicableTo = ra.Amenity.Category.ApplicableTo
                                }).ToList()
                        };
                    }).ToList()
            };

            return new Response<ManagedHotelDetailDto>(dto, "Lấy thông tin chi tiết khách sạn thành công.");
        }
    }
}
