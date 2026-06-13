using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AmenityDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Amenities.Commands
{
    public class UpdateAmenityCategoryCommand : IRequest<Response<AmenityCategoryDto>>
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class UpdateAmenityCategoryCommandHandler
        : IRequestHandler<UpdateAmenityCategoryCommand, Response<AmenityCategoryDto>>
    {
        private readonly IApplicationDbContext _context;
        public UpdateAmenityCategoryCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<AmenityCategoryDto>> Handle(
            UpdateAmenityCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.AmenityCategories.FindAsync(new object[] { request.Id }, cancellationToken)
                ?? throw new ApiException("Không tìm thấy danh mục!");

            var duplicate = await _context.AmenityCategories
                .AnyAsync(c => c.Name.ToLower() == request.Name.ToLower() && c.Id != request.Id, cancellationToken);
            if (duplicate)
                throw new ApiException($"Danh mục '{request.Name}' đã tồn tại!");

            category.Name = request.Name.Trim();
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<AmenityCategoryDto>(
                new AmenityCategoryDto { Id = category.Id, Name = category.Name },
                "Cập nhật danh mục thành công!");
        }
    }
}
