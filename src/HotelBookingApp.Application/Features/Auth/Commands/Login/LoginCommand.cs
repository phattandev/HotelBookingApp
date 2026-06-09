using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Auth.Commands.Login
{
    public class LoginCommand : IRequest<Response<AuthResponseDto>>
    {
        public string UsernameOrEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, Response<AuthResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public LoginCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<Response<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            User? user = await _context.Users.
                Include(u => u.Role).
                FirstOrDefaultAsync(u => u.Username == request.UsernameOrEmail || u.Email == request.UsernameOrEmail, cancellationToken);

            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new ApiException("Tài khoản hoặc mật khẩu không chính xác.");
            }

            string? accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            string? refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Unspecified);
            await _context.SaveChangesAsync(cancellationToken);

            AuthResponseDto? responseData = new AuthResponseDto
            {
                UserId = user.Id.ToString(),
                FullName = user.Username,
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            return new Response<AuthResponseDto>(responseData, "Đăng nhập thành công.");
        }
    }

    public class LoginCommandValidator : AbstractValidator<LoginCommand>
    {
        public LoginCommandValidator()
        {
            RuleFor(p => p.UsernameOrEmail)
                .NotEmpty().WithMessage("Tài khoản hoặc Email không được để trống.");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Mật khẩu không được để trống.");
        }
    }
}
