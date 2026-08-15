using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Commands
{
    public class ReviewBusinessCommand : IRequest<Response<string>>
    {
        public Guid BusinessId { get; set; }
        public string Action { get; set; } = null!;         // "Approve" hoặc "Reject"
        public string? RejectionReason { get; set; }        // Bắt buộc khi Action = "Reject"
    }

    public class ReviewBusinessCommandHandler : IRequestHandler<ReviewBusinessCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public ReviewBusinessCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(ReviewBusinessCommand request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses.FindAsync(new object[] { request.BusinessId }, cancellationToken);
            if (business == null)
                throw new ApiException("Không tìm thấy doanh nghiệp.");

            if (request.Action == "Approve")
            {
                business.VerificationStatus = BusinessVerificationStatus.Approved;
                business.RejectionReason = null;    // Xoá lý do từ chối cũ nếu có
                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new Response<string> { Succeeded = true, Data = "Success", Message = "Đã phê duyệt doanh nghiệp thành công. Đối tác có thể đăng nhập và sử dụng hệ thống." };
            }
            else if (request.Action == "Reject")
            {
                business.VerificationStatus = BusinessVerificationStatus.Rejected;
                business.RejectionReason = request.RejectionReason;
                business.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
                return new Response<string> { Succeeded = true, Data = "Success", Message = "Đã từ chối hồ sơ doanh nghiệp." };
            }
            return new Response<string>("Hành động không hợp lệ.");
        }
    }

    public class ReviewBusinessCommandValidator : AbstractValidator<ReviewBusinessCommand>
    {
        public ReviewBusinessCommandValidator()
        {
            RuleFor(x => x.Action)
                .NotEmpty().WithMessage("Vui lòng cung cấp hành động.")
                .Must(x => x == "Approve" || x == "Reject")
                .WithMessage("Hành động không hợp lệ. Chỉ chấp nhận 'Approve' hoặc 'Reject'.");

            RuleFor(x => x.RejectionReason)
                .NotEmpty().When(x => x.Action == "Reject")
                .WithMessage("Vui lòng nhập lý do từ chối để thông báo cho doanh nghiệp.");
        }
    }
}
