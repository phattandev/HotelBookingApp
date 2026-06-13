using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HotelBookingApp.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using HotelBookingApp.Domain.Models;
using System.Text;

namespace HotelBookingApp.Infrastructure.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly IConfiguration _configuration;

        public JwtTokenGenerator(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateAccessToken(User user)
        {
            IConfigurationSection? jwtSettings = _configuration.GetSection("JwtSettings");
            string? secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("Jwt Secret is missing");

            SymmetricSecurityKey? securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            SigningCredentials? credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512);

            Claim[]? claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? "customer"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            JwtSecurityToken? token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpirationMinutes"] ?? "60")),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            byte[]? randomNumber = new byte[32];
            using RandomNumberGenerator? rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            IConfigurationSection? jwtSettings = _configuration.GetSection("JwtSettings");
            string? secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("Jwt Secret is missing");

            TokenValidationParameters? tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ValidateLifetime = false
            };

            JwtSecurityTokenHandler? tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal? principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha512, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Token không hợp lệ.");
            }

            return principal;
        }
    }
}
