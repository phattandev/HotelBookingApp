using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AmenityDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Amenities.Queries
{
    public class GetAmenitiesQuery : IRequest<Response<IEnumerable<AmenityDto>>>
    {
        public Guid? CategoryId { get; set; }
        public bool? IsActive { get; set; } // null = tất cả, true = đang dùng
    }

    public class GetAmenitiesQueryHandler
        : IRequestHandler<GetAmenitiesQuery, Response<IEnumerable<AmenityDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetAmenitiesQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<IEnumerable<AmenityDto>>> Handle(
            GetAmenitiesQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Amenities.Include(a => a.Category).AsQueryable();

            if (request.CategoryId.HasValue && request.CategoryId != Guid.Empty)
                query = query.Where(a => a.CategoryId == request.CategoryId.Value);

            if (request.IsActive.HasValue)
                query = query.Where(a => a.IsActive == request.IsActive.Value);

            var amenities = await query
                .OrderBy(a => a.Category.Name).ThenBy(a => a.Name)
                .Select(a => new AmenityDto
                {
                    Id = a.Id,
                    CategoryId = a.CategoryId,
                    CategoryName = a.Category.Name,
                    Name = a.Name,
                    IsActive = a.IsActive
                })
                .ToListAsync(cancellationToken);

            return new Response<IEnumerable<AmenityDto>>(amenities, "Lấy danh sách tiện nghi thành công!");
        }
    }
}
