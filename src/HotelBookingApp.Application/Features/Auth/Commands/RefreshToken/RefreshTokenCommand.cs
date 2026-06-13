using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;

namespace HotelBookingApp.Application.Features.Auth.Commands.RefreshToken
{
    public class RefreshTokenCommand : IRequest<Response<AuthResponseDto>>
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }

    public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Response<AuthResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IConfiguration _configuration;

        public RefreshTokenCommandHandler(IApplicationDbContext context, IJwtTokenGenerator jwtTokenGenerator, IConfiguration configuration)
        {
            _context = context;
            _jwtTokenGenerator = jwtTokenGenerator;
            _configuration = configuration;
        }

        public async Task<Response<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
        {
            ClaimsPrincipal? principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
            string? userIdString = principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
            {
                throw new ApiException("Access Token không hợp lệ.");
            }

            User? user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new ApiException("Refresh Token không hợp lệ hoặc đã hết hạn. Vui lòng đăng nhập lại.");
            }

            string? newAccessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            string? newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            string? expiryDaysString = _configuration["JwtSettings:RefreshTokenExpirationDays"];
            int refreshTokenExpirationDays = 7;
            if (int.TryParse(expiryDaysString, out int parsedDays))
            {
                refreshTokenExpirationDays = parsedDays;
            }

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(refreshTokenExpirationDays), DateTimeKind.Unspecified);
            await _context.SaveChangesAsync(cancellationToken);

            AuthResponseDto? responseData = new AuthResponseDto
            {
                UserId = user.Id.ToString(),
                FullName = user.Username,
                Email = user.Email,
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };

            return new Response<AuthResponseDto>(responseData, "Refresh Token thành công.");
        }
    }
}
