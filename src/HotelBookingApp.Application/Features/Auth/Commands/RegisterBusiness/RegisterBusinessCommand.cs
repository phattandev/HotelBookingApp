using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Auth.Commands.RegisterBusiness
{
    public class RegisterBusinessCommand : IRequest<Response<AuthResponseDto>>
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

    public class RegisterBusinessCommandHandler : IRequestHandler<RegisterBusinessCommand, Response<AuthResponseDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public RegisterBusinessCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtTokenGenerator)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<Response<AuthResponseDto>> Handle(RegisterBusinessCommand request, CancellationToken cancellationToken)
        {
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.RepresentativeEmail, cancellationToken);
            if (emailExists)
            {
                throw new ApiException("Email làm việc này đã được đăng ký trong hệ thống giám sát.");
            }

            // Tạo bản ghi User cơ sở với quyền hệ thống là 'partner'
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "partner", cancellationToken);
            if (role == null) throw new ApiException("Hệ thống chưa thiết lập Role 'partner' trong CSDL.");

            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = request.BusinessName,
                Email = request.RepresentativeEmail,
                Phone = request.RepresentativePhone,
                PasswordHash = _passwordHasher.HashPasswordEnhanced(request.Password),
                RoleId = role.Id, // Gán phân quyền Partner phục vụ cho thiết kế PartnerLayout định sẵn
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            user.Role = role;
            /* 💡 GỢI Ý PHÁT TRIỂN KIẾN TRÚC CHO LUẬN VĂN THÀNH CÔNG:
               Để lưu trữ trọn vẹn các trường mở rộng (TaxCode, BusinessAddress, RepresentativeName, Position), bạn nên:
               - Cách 1: Thêm các cột tương ứng vào thực thể `User` tại lớp Domain và tạo Migration mới.
               - Cách 2: Tạo một thực thể độc lập tên là `BusinessProfile` liên kết khóa ngoại 1-1 tới bảng `User`.
               
               Khi bạn đã cập nhật cấu trúc cơ sở dữ liệu theo một trong hai cách trên, hãy viết bổ sung logic nạp dữ liệu tại đây:
               VD (Theo cách 1):
               user.TaxCode = request.TaxCode;
               user.BusinessAddress = request.BusinessAddress;
            */

            string accessToken = _jwtTokenGenerator.GenerateAccessToken(user);
            string refreshToken = _jwtTokenGenerator.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(7), DateTimeKind.Unspecified);

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

            return new Response<AuthResponseDto>(responseData, "Yêu cầu đăng ký tài khoản doanh nghiệp đối tác đã được gửi lên hệ thống.");
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
                .EmailAddress().WithMessage("Định dạng thư điện tử email làm việc không đúng.");

            RuleFor(p => p.Password)
                .NotEmpty().WithMessage("Mật khẩu tài khoản không được để trống.")
                .MinimumLength(6).WithMessage("Mật khẩu bảo mật phải tối thiểu từ 6 ký tự trở lên.");

            RuleFor(p => p.ConfirmPassword)
                .NotEmpty().WithMessage("Xác nhận mật khẩu thiết lập không được trống.")
                .Equal(p => p.Password).WithMessage("Xác nhận mật khẩu doanh nghiệp không trùng khớp.");
        }
    }
}