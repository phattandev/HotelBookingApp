using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;

namespace HotelBookingApp.Application.Features.Hotels.Commands
{
    public class ReviewHotelCommand : IRequest<Response<string>>
    {
        public Guid HotelId { get; set; }
        /// <summary>"Approve" hoặc "Reject"</summary>
        public string Action { get; set; } = null!;
        /// <summary>Bắt buộc khi Action = "Reject"</summary>
        public string? RejectionReason { get; set; }
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
                hotel.RejectionReason = null;   // Xoá lý do cũ nếu có
                hotel.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new Response<string> { Succeeded = true, Data = "Success", Message = $"Đã phê duyệt khách sạn '{hotel.Name}'. Khách sạn hiện đang hoạt động." };
            }
            else if (request.Action.Equals("reject", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(request.RejectionReason))
                    throw new ApiException("Vui lòng nhập lý do từ chối để thông báo cho đối tác.");

                hotel.ApprovalStatus = HotelApprovalStatus.Rejected;
                hotel.IsActive = false;
                hotel.RejectionReason = request.RejectionReason;
                hotel.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new Response<string> { Succeeded = true, Data = "Success", Message = $"Đã từ chối hồ sơ khách sạn '{hotel.Name}'." };
            }

            throw new ApiException("Hành động không hợp lệ. Chỉ chấp nhận 'approve' hoặc 'reject'.");
        }
    }

    public class ReviewHotelCommandValidator : AbstractValidator<ReviewHotelCommand>
    {
        public ReviewHotelCommandValidator()
        {
            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("Hành động không được để trống.");
        }
    }
}
