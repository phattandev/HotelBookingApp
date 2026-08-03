using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    public class GetPublicRoomTypeDetailQuery : IRequest<Response<RoomTypePublicDto>>
    {
        public Guid RoomTypeId { get; set; }
        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }
    }

    public class GetPublicRoomTypeDetailQueryHandler : IRequestHandler<GetPublicRoomTypeDetailQuery, Response<RoomTypePublicDto>>
    {
        private readonly IApplicationDbContext _context;
        public GetPublicRoomTypeDetailQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<RoomTypePublicDto>> Handle(GetPublicRoomTypeDetailQuery request, CancellationToken cancellationToken)
        {
            var rt = await _context.RoomTypes
                .Include(rt => rt.Hotel)
                .Include(rt => rt.Images)
                .Include(rt => rt.RoomTypeAmenities)
                    .ThenInclude(ra => ra.Amenity)
                        .ThenInclude(a => a.Category)
                .FirstOrDefaultAsync(r => r.Id == request.RoomTypeId && r.IsActive && r.Hotel.ApprovalStatus == Domain.Models.HotelApprovalStatus.Approved && r.Hotel.IsActive, cancellationToken);

            if (rt == null)
                throw new ApiException("Không tìm thấy loại phòng hoặc loại phòng không hoạt động.");

            var vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh");
            var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
            var today = DateOnly.FromDateTime(nowVn);

            var checkInDate = request.CheckIn ?? today;
            var checkOutDate = request.CheckOut ?? checkInDate.AddDays(1);

            var bookedRooms = await _context.Bookings
                .Where(b =>
                    b.RoomTypeId == rt.Id &&
                    (b.Status == Domain.Models.BookingStatus.Pending || b.Status == Domain.Models.BookingStatus.Approved || b.Status == Domain.Models.BookingStatus.Confirmed) &&
                    b.CheckInDate < checkOutDate &&
                    b.CheckOutDate > checkInDate)
                .SumAsync(b => (int?)b.NumRooms, cancellationToken) ?? 0;

            int availableRooms = Math.Max(0, rt.TotalRooms - bookedRooms);

            var dto = new RoomTypePublicDto
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
            };

            return new Response<RoomTypePublicDto>(dto, "Lấy chi tiết loại phòng thành công.");
        }
    }
}
