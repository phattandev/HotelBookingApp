using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Auth.Commands.RegisterUser
{
    public class RegisterUserCommand : IRequest<Response<AuthResponseDto>>
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string ConfirmPassword { get; set; } = null!;
    }

    public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, Response<AuthResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public RegisterUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<Response<AuthResponseDto>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Email trùng lặp
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
            if (emailExists)
            {
                throw new ApiException("Email này đã được sử dụng bởi một tài khoản khác.");
            }

            // 2. Khởi tạo đối tượng User mới
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "customer", cancellationToken);
            if (role == null) throw new ApiException("Hệ thống chưa thiết lập Role 'customer' trong CSDL.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.Email, // Đặt Email làm Username để đồng bộ luồng Đăng nhập
                Email = request.Email,
                FullName = request.Email.Split('@')[0],
                PasswordHash = _passwordHasher.HashPasswordEnhanced(request.Password),
                Phone = string.Empty,
                RoleId = role.Id, // Phân quyền mặc định cho khách hàng vãng lai
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.Role = role;

            // 3. Cấp phát Token tự động đăng nhập ngay sau khi đăng ký
            string accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            string refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            var responseData = new AuthResponseDto
            {
                UserId = user.Id.ToString(),
                FullName = user.Username,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            return new Response<AuthResponseDto>(responseData, "Đăng ký tài khoản thành công.");
        }
    }

    public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
    {
        public RegisterUserCommandValidator()
        {
            RuleFor(p => p.Email)
                .NotEmpty().WithMessage("Email không được để trống.")
                .EmailAddress().WithMessage("Định dạng Email không hợp lệ.");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu phải có ít nhất 6 ký tự.");

            RuleFor(p => p.ConfirmPassword)
                .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
                .Equal(p => p.Password).WithMessage("Mật khẩu xác nhận không trùng khớp với mật khẩu đã nhập.");
        }
    }
}