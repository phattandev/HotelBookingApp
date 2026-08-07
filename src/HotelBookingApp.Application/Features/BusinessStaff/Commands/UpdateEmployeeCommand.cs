using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.BusinessStaff.Commands
{
    public class UpdateEmployeeCommand : IRequest<Response<string>>
    {
        public Guid PartnerId { get; set; }  // Gán từ Controller qua JWT Token
        public Guid EmployeeId { get; set; } // Gán từ route param

        public string FullName { get; set; } = null!;
        public string Phone { get; set; } = null!;

        /// <summary>Nếu để trống (null hoặc empty) sẽ không đổi mật khẩu.</summary>
        public string? NewPassword { get; set; }
    }

    public class UpdateEmployeeCommandHandler : IRequestHandler<UpdateEmployeeCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public UpdateEmployeeCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<Response<string>> Handle(UpdateEmployeeCommand request, CancellationToken cancellationToken)
        {
            // Xác thực: nhân viên phải thuộc doanh nghiệp của Partner đang đăng nhập
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null)
                throw new ApiException("Hồ sơ doanh nghiệp không tồn tại.");

            var staffRecord = await _context.BusinessStaff
                .FirstOrDefaultAsync(s => s.UserId == request.EmployeeId && s.BusinessId == business.Id, cancellationToken);
            if (staffRecord == null)
                throw new ApiException("Nhân viên không thuộc doanh nghiệp của bạn.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.EmployeeId, cancellationToken);
            if (user == null)
                throw new ApiException("Tài khoản nhân viên không tồn tại.");

            // Cập nhật thông tin cơ bản
            user.FullName = request.FullName;
            user.Phone = request.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            // Chỉ đổi mật khẩu nếu Partner nhập giá trị mới
            if (!string.IsNullOrWhiteSpace(request.NewPassword))
            {
                user.PasswordHash = _passwordHasher.HashPasswordEnhanced(request.NewPassword);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string>($"Đã cập nhật thông tin nhân viên '{user.FullName}' thành công.");
        }
    }

    public class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
    {
        public UpdateEmployeeCommandValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống.")
                .MaximumLength(100).WithMessage("Họ tên tối đa 100 ký tự.");

            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Số điện thoại không được để trống.")
                .MaximumLength(15).WithMessage("Số điện thoại tối đa 15 ký tự.");

            // Mật khẩu không bắt buộc nhưng nếu nhập phải đủ 6 ký tự
            RuleFor(x => x.NewPassword)
                .MinimumLength(6).WithMessage("Mật khẩu mới phải có ít nhất 6 ký tự.")
                .When(x => !string.IsNullOrWhiteSpace(x.NewPassword));
        }
    }
}
