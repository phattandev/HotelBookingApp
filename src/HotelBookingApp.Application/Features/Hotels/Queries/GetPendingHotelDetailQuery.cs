using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    public class GetPendingHotelDetailQuery : IRequest<Response<ManagedHotelDetailDto>>
    {
        public Guid HotelId { get; set; }
    }

    public class GetPendingHotelDetailQueryHandler : IRequestHandler<GetPendingHotelDetailQuery, Response<ManagedHotelDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetPendingHotelDetailQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<ManagedHotelDetailDto>> Handle(GetPendingHotelDetailQuery request, CancellationToken cancellationToken)
        {
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
                .FirstOrDefaultAsync(h => h.Id == request.HotelId, cancellationToken);

            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn.");

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var activeBookingItems = await _context.BookingItems
                .Include(bi => bi.Booking)
                .Where(bi => bi.Booking.HotelId == hotel.Id &&
                             (bi.Booking.Status == Domain.Models.BookingStatus.Pending || bi.Booking.Status == Domain.Models.BookingStatus.Approved || bi.Booking.Status == Domain.Models.BookingStatus.Confirmed) &&
                             bi.Booking.CheckInDate <= today &&
                             bi.Booking.CheckOutDate > today)
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
                        var bookedRooms = activeBookingItems.Where(bi => bi.RoomTypeId == rt.Id).Sum(bi => bi.NumRooms);
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
