using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AmenityDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Amenities.Commands
{
    public class CreateAmenityCategoryCommand : IRequest<Response<AmenityCategoryDto>>
    {
        public string Name { get; set; } = null!;
    }

    public class CreateAmenityCategoryCommandHandler
        : IRequestHandler<CreateAmenityCategoryCommand, Response<AmenityCategoryDto>>
    {
        private readonly IApplicationDbContext _context;
        public CreateAmenityCategoryCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<AmenityCategoryDto>> Handle(
            CreateAmenityCategoryCommand request, CancellationToken cancellationToken)
        {
            var duplicate = await _context.AmenityCategories
                .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower(), cancellationToken);
            if (duplicate)
                throw new ApiException($"Danh mục '{request.Name}' đã tồn tại!");

            var category = new AmenityCategory { Id = Guid.NewGuid(), Name = request.Name.Trim() };
            _context.AmenityCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<AmenityCategoryDto>(
                new AmenityCategoryDto { Id = category.Id, Name = category.Name },
                $"Đã tạo danh mục '{category.Name}'!");
        }
    }
}
