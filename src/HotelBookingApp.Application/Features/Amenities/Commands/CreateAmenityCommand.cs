using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AmenityDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Amenities.Commands
{
    public class CreateAmenityCommand : IRequest<Response<AmenityDto>>
    {
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
    }

    public class CreateAmenityCommandHandler
        : IRequestHandler<CreateAmenityCommand, Response<AmenityDto>>
    {
        private readonly IApplicationDbContext _context;
        public CreateAmenityCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<AmenityDto>> Handle(
            CreateAmenityCommand request, CancellationToken cancellationToken)
        {
            var categoryExists = await _context.AmenityCategories
                .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
            if (!categoryExists)
                throw new ApiException("Danh mục tiện nghi không tồn tại!");

            var duplicate = await _context.Amenities.AnyAsync(
                a => a.CategoryId == request.CategoryId && a.Name.ToLower() == request.Name.ToLower(),
                cancellationToken);
            if (duplicate)
                throw new ApiException($"Tiện nghi '{request.Name}' đã tồn tại trong danh mục này!");

            var amenity = new Amenity
            {
                Id = Guid.NewGuid(),
                CategoryId = request.CategoryId,
                Name = request.Name.Trim(),
                IsActive = true
            };
            _context.Amenities.Add(amenity);
            await _context.SaveChangesAsync(cancellationToken);

            var categoryName = (await _context.AmenityCategories.FindAsync(new object[] { request.CategoryId }, cancellationToken))!.Name;

            return new Response<AmenityDto>(new AmenityDto
            {
                Id = amenity.Id,
                CategoryId = amenity.CategoryId,
                CategoryName = categoryName,
                Name = amenity.Name,
                IsActive = amenity.IsActive
            }, $"Đã thêm tiện nghi '{amenity.Name}'!");
        }
    }
}
