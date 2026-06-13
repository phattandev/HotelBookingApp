using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Amenities.Commands
{
    public class DeleteAmenityCategoryCommand : IRequest<Response<string>>
    {
        public Guid Id { get; set; }
    }

    public class DeleteAmenityCategoryCommandHandler
        : IRequestHandler<DeleteAmenityCategoryCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public DeleteAmenityCategoryCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(
            DeleteAmenityCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _context.AmenityCategories
                .Include(c => c.Amenities)
                .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
                ?? throw new ApiException("Không tìm thấy danh mục!");

            if (category.Amenities.Any())
                throw new ApiException(
                    $"Không thể xóa danh mục '{category.Name}' vì còn {category.Amenities.Count} tiện nghi bên trong. Hãy xóa hoặc chuyển tiện nghi trước.");

            _context.AmenityCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string>($"Đã xóa danh mục '{category.Name}'!", "Xóa thành công!");
        }
    }
}
