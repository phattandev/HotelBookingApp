using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Commands
{
    public class UpdateHotelInfoCommand : IRequest<Response<string>>
    {
        public Guid ManagerId { get; set; } // Gán từ JWT
        public string? Description { get; set; }
        public int? StarRating { get; set; }
    }

    public class UpdateHotelInfoCommandHandler : IRequestHandler<UpdateHotelInfoCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public UpdateHotelInfoCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(UpdateHotelInfoCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null)
                throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var hotel = await _context.Hotels.FindAsync(new object[] { assignment.HotelId }, cancellationToken);
            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn.");

            hotel.Description = request.Description;
            hotel.StarRating = request.StarRating;

            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string>("Cập nhật thông tin khách sạn thành công.");
        }
    }

    public class UpdateHotelInfoCommandValidator : AbstractValidator<UpdateHotelInfoCommand>
    {
        public UpdateHotelInfoCommandValidator()
        {
            RuleFor(x => x.StarRating)
                .InclusiveBetween(1, 5).When(x => x.StarRating.HasValue)
                .WithMessage("Số sao phải từ 1 đến 5.");

            RuleFor(x => x.Description)
                .MaximumLength(5000).When(x => x.Description != null)
                .WithMessage("Mô tả tối đa 5000 ký tự.");
        }
    }
}
