using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AmenityDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Amenities.Queries
{
    public class GetAmenityCategoriesQuery : IRequest<Response<IEnumerable<AmenityCategoryDto>>> { }

    public class GetAmenityCategoriesQueryHandler
        : IRequestHandler<GetAmenityCategoriesQuery, Response<IEnumerable<AmenityCategoryDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetAmenityCategoriesQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<IEnumerable<AmenityCategoryDto>>> Handle(
            GetAmenityCategoriesQuery request, CancellationToken cancellationToken)
        {
            var categories = await _context.AmenityCategories
                .OrderBy(c => c.Name)
                .Select(c => new AmenityCategoryDto { Id = c.Id, Name = c.Name })
                .ToListAsync(cancellationToken);

            return new Response<IEnumerable<AmenityCategoryDto>>(categories, "Lấy danh mục tiện nghi thành công!");
        }
    }
}
