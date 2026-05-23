using System.Security.Claims;
using HotelBookingApp.Infrastructure;

namespace HotelBookingApp.Application.Common.Interfaces
{
    public interface IPasswordHasher
    {
        string HashPassword(string password);
        bool VerifyPassword(string password, string passwordHash);
        string HashPasswordEnhanced(string password);
        bool VerifyPasswordEnhanced(string password, string passwordHash);
    }
}
