using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
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
            // 1. So sánh không phân biệt hoa thường (Fix lỗi Case-Sensitive của PostgreSQL)
            User? user = await _context.Users.
                Include(u => u.Role).
                FirstOrDefaultAsync(u =>
                    u.Username.ToLower() == request.UsernameOrEmail.ToLower() ||
                    u.Email.ToLower() == request.UsernameOrEmail.ToLower(),
                cancellationToken);
            
            

            // 2. Kiểm tra sai thông tin đăng nhập
            if (user == null || !_passwordHasher.VerifyPasswordEnhanced(request.Password, user.PasswordHash))
            {
                throw new ApiException("Tài khoản hoặc mật khẩu không chính xác.");
            }

            if (!user.IsActive)
            {
                throw new ApiException("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ với Chủ doanh nghiệp hoặc Quản trị viên.");
            }

            // 3. Kiểm tra Doanh nghiệp đối với role Partner
            if (user.Role != null && user.Role.Name.ToLower() == "partner")
            {
                var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == user.Id, cancellationToken);

                // Nếu tài khoản partner mà không có hồ sơ DN → bất thường, không cho đăng nhập
                if (business == null)
                {
                    throw new ApiException("Tài khoản doanh nghiệp của bạn chưa có hồ sơ hợp lệ. Vui lòng liên hệ quản trị viên.");
                }

                if (business.VerificationStatus == BusinessVerificationStatus.Pending)
                {
                    throw new ApiException("Tài khoản doanh nghiệp của bạn đang chờ Admin phê duyệt.");
                }
                else if (business.VerificationStatus == BusinessVerificationStatus.Rejected)
                {
                    var reason = string.IsNullOrWhiteSpace(business.RejectionReason)
                        ? "Không có lý do cụ thể."
                        : business.RejectionReason;
                    throw new ApiException($"Hồ sơ doanh nghiệp của bạn đã bị từ chối phê duyệt. Lý do: {reason}");
                }
            }

            // 4. Sinh Token
            string? accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            string? refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;

            // 5. Fix lỗi DateTime cho PostgreSQL (Chỉ dùng UtcNow, không dùng SpecifyKind)
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

            await _context.SaveChangesAsync(cancellationToken);

            AuthResponseDto? responseData = new AuthResponseDto
            {
                UserId = user.Id.ToString(),
                FullName = user.FullName ?? user.Username, // Ưu tiên trả về FullName hiển thị cho đẹp
                Email = user.Email,
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };

            return new Response<AuthResponseDto>(responseData, "Đăng nhập thành công.");
        }
    }

}
