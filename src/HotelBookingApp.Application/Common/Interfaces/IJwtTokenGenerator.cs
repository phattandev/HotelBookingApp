using System.Security.Claims;
using HotelBookingApp.Domain.Models;
using HotelBookingApp.Infrastructure;

namespace HotelBookingApp.Application.Common.Interfaces
{
    public interface IJwtTokenGenerator
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}
