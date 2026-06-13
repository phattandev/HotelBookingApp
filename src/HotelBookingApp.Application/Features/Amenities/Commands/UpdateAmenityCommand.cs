using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AmenityDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Amenities.Commands
{
    public class UpdateAmenityCommand : IRequest<Response<AmenityDto>>
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    public class UpdateAmenityCommandHandler
        : IRequestHandler<UpdateAmenityCommand, Response<AmenityDto>>
    {
        private readonly IApplicationDbContext _context;
        public UpdateAmenityCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<AmenityDto>> Handle(
            UpdateAmenityCommand request, CancellationToken cancellationToken)
        {
            var amenity = await _context.Amenities.Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
                ?? throw new ApiException("Không tìm thấy tiện nghi!");

            var duplicate = await _context.Amenities.AnyAsync(
                a => a.CategoryId == request.CategoryId
                     && a.Name.ToLower() == request.Name.ToLower()
                     && a.Id != request.Id,
                cancellationToken);
            if (duplicate)
                throw new ApiException($"Tiện nghi '{request.Name}' đã tồn tại trong danh mục này!");

            amenity.CategoryId = request.CategoryId;
            amenity.Name = request.Name.Trim();
            amenity.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            var category = await _context.AmenityCategories
    .FindAsync(new object[] { request.CategoryId }, cancellationToken)
    ?? throw new ApiException("Danh mục tiện nghi không tồn tại!");

            amenity.CategoryId = request.CategoryId;
            amenity.Name = request.Name.Trim();
            amenity.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<AmenityDto>(new AmenityDto
            {
                Id = amenity.Id,
                CategoryId = amenity.CategoryId,
                CategoryName = category.Name,
                Name = amenity.Name,
                IsActive = amenity.IsActive
            }, "Cập nhật tiện nghi thành công!");
        }
    }
}
