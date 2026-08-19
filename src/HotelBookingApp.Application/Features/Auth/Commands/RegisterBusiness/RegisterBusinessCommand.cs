using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
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

        // Tài liệu pháp lý
        public List<IFormFile>? Documents { get; set; } = new();
    }

    public class RegisterBusinessCommandHandler : IRequestHandler<RegisterBusinessCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICloudinaryService _cloudinaryService;

        public RegisterBusinessCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _passwordHasher = passwordHasher;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<Response<string>> Handle(RegisterBusinessCommand request, CancellationToken cancellationToken)
        {
            // 1. Kiểm tra Email tồn tại
            var emailExists = await _context.Users.AnyAsync(u => u.Email == request.RepresentativeEmail, cancellationToken);
            if (emailExists)
            {
                throw new ApiException("Email đã được đăng ký.");
            }

            // 1.5. Kiểm tra MST tồn tại
            var taxCodeExists = await _context.Businesses.AnyAsync(b => b.TaxCode == request.TaxCode, cancellationToken);
            if (taxCodeExists)
            {
                throw new ApiException("Mã số thuế này đã được đăng ký trên hệ thống.");
            }

            // 2. Lấy Role 'partner'
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "partner", cancellationToken);
            if (role == null) throw new ApiException("Hệ thống chưa thiết lập Role 'partner' trong CSDL.");

            // 3. Khởi tạo User (Chủ doanh nghiệp)
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = request.RepresentativeEmail,
                Email = request.RepresentativeEmail,
                FullName = request.RepresentativeName, // Bổ sung FullName bắt buộc từ Entity User
                Phone = request.RepresentativePhone,
                PasswordHash = _passwordHasher.HashPasswordEnhanced(request.Password),
                RoleId = role.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // 4. Validate files (nếu có lỗi thì throw exception trước khi tạo user)
            const int maxFileCount = 10;
            const long maxFileSizeBytes = 10 * 1024 * 1024; // 10MB

            if (request.Documents != null && request.Documents.Count > 0)
            {
                if (request.Documents.Count > maxFileCount)
                    throw new ApiException($"Tối đa {maxFileCount} file được phép upload.");

                foreach (var file in request.Documents)
                {
                    if (file.Length > maxFileSizeBytes)
                        throw new ApiException($"File '{file.FileName}' vượt quá giới hạn 10MB.");
                    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (ext != ".pdf")
                        throw new ApiException($"File '{file.FileName}' không hợp lệ. Chỉ chấp nhận file PDF.");
                }
            }

            // 5. Khởi tạo Business (Hồ sơ doanh nghiệp)
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

            // 6. Upload file và tạo BusinessDocument records
            if (request.Documents != null && request.Documents.Count > 0)
            {
                var folder = $"business-documents/{business.Id}";
                foreach (var file in request.Documents)
                {
                    await using var stream = file.OpenReadStream();
                    var uploadResult = await _cloudinaryService.UploadRawFileAsync(stream, file.FileName, folder);
                    
                    _context.BusinessDocuments.Add(new BusinessDocument
                    {
                        Id = Guid.NewGuid(),
                        BusinessId = business.Id,
                        FileName = file.FileName,
                        FileUrl = uploadResult.Url,
                        PublicId = uploadResult.PublicId,
                        FileSizeBytes = file.Length,
                        UploadedAt = DateTime.UtcNow
                    });
                }
            }

            // 7. Lưu vào DB (EF Core sẽ tự động bọc trong Transaction)
            _context.Users.Add(user);
            _context.Businesses.Add(business);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string> { Succeeded = true, Data = "Success", Message = "Đăng ký doanh nghiệp thành công! Vui lòng chờ Admin phê duyệt." };
        }
    }
}