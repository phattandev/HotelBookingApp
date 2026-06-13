using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using FluentValidation;

namespace HotelBookingApp.Application.Features.Hotels.Commands
{
    public class ReviewHotelCommand : IRequest<Response<string>>
    {
        public Guid HotelId { get; set; }
        /// <summary>"Approve" hoặc "Reject"</summary>
        public string Action { get; set; } = null!;
    }

    public class ReviewHotelCommandHandler : IRequestHandler<ReviewHotelCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public ReviewHotelCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(ReviewHotelCommand request, CancellationToken cancellationToken)
        {
            var hotel = await _context.Hotels.FindAsync(new object[] { request.HotelId }, cancellationToken);
            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn.");

            if (request.Action.Equals("approve", StringComparison.OrdinalIgnoreCase))
            {
                hotel.ApprovalStatus = HotelApprovalStatus.Approved;
                hotel.IsActive = true;
            }
            else
            {
                hotel.ApprovalStatus = HotelApprovalStatus.Rejected;
                hotel.IsActive = false;
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string>($"Đã {request.Action} khách sạn {hotel.Name}.");
        }
    }

    public class ReviewHotelCommandValidator : AbstractValidator<ReviewHotelCommand>
    {
        private static readonly string[] ValidActions = { "approve", "reject" };

        public ReviewHotelCommandValidator()
        {
            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("Ġành động không được để trống.")
                .Must(a => ValidActions.Contains(a.ToLower()))
                .WithMessage("Ġành động chỉ chấp nhận 'approve' hoặc 'reject'.");
        }
    }
}
