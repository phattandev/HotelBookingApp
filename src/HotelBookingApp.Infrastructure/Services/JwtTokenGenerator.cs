using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using HotelBookingApp.Application.Common.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
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
            string? secretKey = jwtSettings["SecretKey"] ?? "sGraw5@|K1aQFW+?fo.T*/fBI)4Jy8P60:wdRtncyO@KFme/2J&toDLz!U#/x$4kb6hIkq16Boo.wx(elXB>EySOik!^Vz%!%!L2URXr&8Ksmj*oWt&7As(b:jut9+|VUBM9OcJtfco[1Hzq;TsBY+kasYrzvu?Tm4FUcLvm9$EWW#A:Iv3fD{CE$f>uI4WKlA7zDrJJehF.f[|4CbA%k#e^v5A.[$J]vyo[wu%C=p1G[Q#%G{rrxJxCaD?c5}o}slmG1L1>&)xaRgGHUzU-)t,JtLzx?eMo=eqptS&{@OkQ=Z)PSorxKzaP=@I:w<0=U*d3lC+)plY,;$<pss)uvE1>jb8m?!$czGc]52sC,C{tmmRgd@)bQqybG&%GY).[e}8kGWk5-@86GA[WOy|7KmA}%Udbcv.X5)_3.-7xiq6,{=,4WVCrc#-:[8:/2&)Y;inTJDuqjgy@UNRN5/1zh;rA{$JGVPvOG7E<{nb*Gl%w,2K)ws7;Rp00:lNd-xC[";

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
