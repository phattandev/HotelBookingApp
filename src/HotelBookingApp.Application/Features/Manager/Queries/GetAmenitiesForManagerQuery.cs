using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Queries
{
    /// <summary>Lấy tất cả tiện nghi từ admin để manager chọn (phân loại hotel/room).</summary>
    public class GetAmenitiesForManagerQuery : IRequest<Response<List<AmenityDto>>>
    {
        /// <summary>Lọc theo loại: "hotel", "room", hoặc null để lấy tất cả</summary>
        public string? ApplicableTo { get; set; }
        public GetAmenitiesForManagerQuery(string? applicableTo = null) => ApplicableTo = applicableTo;
    }

    public class GetAmenitiesForManagerQueryHandler : IRequestHandler<GetAmenitiesForManagerQuery, Response<List<AmenityDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetAmenitiesForManagerQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<AmenityDto>>> Handle(GetAmenitiesForManagerQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Amenities
                .Include(a => a.Category)
                .Where(a => a.IsActive != false); // Nếu null cũng coi như active (tránh lỗi dữ liệu cũ)

            if (!string.IsNullOrEmpty(request.ApplicableTo))
            {
                var targetType = request.ApplicableTo.ToLower();
                query = query.Where(a =>
                    a.Category.ApplicableTo == null ||
                    a.Category.ApplicableTo.ToLower() == targetType ||
                    a.Category.ApplicableTo.ToLower() == "both");
            }

            var amenities = await query
                .OrderBy(a => a.Category.Name)
                .ThenBy(a => a.Name)
                .Select(a => new AmenityDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    CategoryName = a.Category.Name,
                    ApplicableTo = a.Category.ApplicableTo
                })
                .ToListAsync(cancellationToken);

            return new Response<List<AmenityDto>>(amenities, "Lấy danh sách tiện nghi thành công.");
        }
    }
}
