using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Queries
{
    /// <summary>Lấy thông tin đầy đủ của khách sạn đang được manager quản lý.</summary>
    public class GetMyManagedHotelQuery : IRequest<Response<ManagedHotelDetailDto>>
    {
        public Guid ManagerId { get; set; }
        public GetMyManagedHotelQuery(Guid managerId) => ManagerId = managerId;
    }

    public class GetMyManagedHotelQueryHandler : IRequestHandler<GetMyManagedHotelQuery, Response<ManagedHotelDetailDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetMyManagedHotelQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<ManagedHotelDetailDto>> Handle(GetMyManagedHotelQuery request, CancellationToken cancellationToken)
        {
            // Lấy phân công đang active của manager
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null)
                throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var hotel = await _context.Hotels
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
                .FirstOrDefaultAsync(h => h.Id == assignment.HotelId, cancellationToken);

            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn.");

            var dto = new ManagedHotelDetailDto
            {
                Id = hotel.Id,
                Name = hotel.Name,
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
                    .Select(rt => new RoomTypeDto
                    {
                        Id = rt.Id,
                        Name = rt.Name,
                        BasePrice = rt.BasePrice,
                        MaxAdults = rt.MaxAdults,
                        MaxChildren = rt.MaxChildren,
                        TotalRooms = rt.TotalRooms,
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
                    }).ToList()
            };

            return new Response<ManagedHotelDetailDto>(dto, "Lấy thông tin khách sạn thành công.");
        }
    }
}
