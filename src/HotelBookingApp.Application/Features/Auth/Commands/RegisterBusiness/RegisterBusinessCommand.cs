using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Auth.Commands.RegisterBusiness
{
    public class RegisterBusinessCommand : IRequest<Response<string>>
    {
        // Nhóm thông tin doanh nghiệp
        public string BusinessName { get; set; } = null!;
        public string TaxCode { get; set; } = null!;
        public string BusinessAddress { get; set; } = null!;

        // Nhóm thông tin người đại diện tài khoản
        public string RepresentativeName { get; set; } = null!;
        public string Position { get; set; } = null!;
        public string RepresentativePhone { get; set; } = null!;
        public string RepresentativeEmail { get; set; } = null!;

        // Thiết lập mật khẩu bảo mật
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }

    public class RegisterBusinessCommandHandler : IRequestHandler<RegisterBusinessCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;

        public RegisterBusinessCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        public async Task<Response<string>> Handle(RegisterBusinessCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Email tồn tại
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.RepresentativeEmail, cancellationToken);
            if (emailExists)
            {
                throw new ApiException("Email làm việc này đã được đăng ký trong hệ thống giám sát.");
            }

            // 2. Lấy Role 'partner'
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "partner", cancellationToken);
            if (role == null) throw new ApiException("Hệ thống chưa thiết lập Role 'partner' trong CSDL.");

            // 3. Khởi tạo User (Chủ doanh nghiệp)
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = request.BusinessName, // Lưu ý: Username phải unique, nếu trùng tên Business sẽ lỗi, có thể cân nhắc dùng Email làm Username như bên RegisterUser
                Email = request.RepresentativeEmail,
                FullName = request.RepresentativeName, // Bổ sung FullName bắt buộc từ Entity User
                Phone = request.RepresentativePhone,
                PasswordHash = _passwordHasher.HashPasswordEnhanced(request.Password),
                RoleId = role.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 4. Khởi tạo Business (Hồ sơ doanh nghiệp)
            var business = new Business
            {
                Id = Guid.NewGuid(),
                OwnerId = userId, // Liên kết 1-1 qua khóa ngoại
                BusinessName = request.BusinessName,
                TaxCode = request.TaxCode,
                BusinessAddress = request.BusinessAddress,
                RepresentativeName = request.RepresentativeName,
                Position = request.Position,
                VerificationStatus = BusinessVerificationStatus.Pending // Chờ Admin duyệt nếu cần
            };

            // 5. Lưu vào DB (EF Core sẽ tự động bọc trong Transaction)
            _context.Users.Add(user);
            _context.Businesses.Add(business);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string>("Đăng ký doanh nghiệp thành công! Vui lòng chờ Admin phê duyệt trước khi đăng nhập.");
        }
    }

    public class RegisterBusinessCommandValidator : AbstractValidator<RegisterBusinessCommand>
    {
        public RegisterBusinessCommandValidator()
        {
            RuleFor(p => p.BusinessName).NotEmpty().WithMessage("Tên doanh nghiệp không được bỏ trống.");
            RuleFor(p => p.TaxCode).NotEmpty().WithMessage("Mã số thuế bắt buộc phải khai báo.");
            RuleFor(p => p.BusinessAddress).NotEmpty().WithMessage("Địa chỉ đăng ký kinh doanh không hợp lệ nếu để trống.");

            RuleFor(p => p.RepresentativeName).NotEmpty().WithMessage("Vui lòng cung cấp họ tên người đại diện.");
            RuleFor(p => p.Position).NotEmpty().WithMessage("Chức vụ người đại diện không được để trống.");

            RuleFor(p => p.RepresentativePhone)
                .NotEmpty().WithMessage("Số điện thoại liên lạc không được trống.")
                .Matches(@"^\d{10,11}$").WithMessage("Số điện thoại người đại diện phải gồm từ 10 đến 11 chữ số.");

            RuleFor(p => p.RepresentativeEmail)
                .NotEmpty().WithMessage("Email làm việc bắt buộc nhập.")
                .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Email không hợp lệ (phải có @ và tên miền hợp lệ).");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Mật khẩu tài khoản không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu bảo mật phải tối thiểu từ 6 ký tự trở lên.");

            RuleFor(p => p.ConfirmPassword)
                .NotEmpty().WithMessage("Xác nhận mật khẩu thiết lập không được trống.")
                .Equal(p => p.Password).WithMessage("Xác nhận mật khẩu doanh nghiệp không trùng khớp.");
        }
    }
}