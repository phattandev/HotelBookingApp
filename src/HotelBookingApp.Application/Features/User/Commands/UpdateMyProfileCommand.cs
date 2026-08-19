using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Users.Commands
{
    public class UpdateMyProfileCommand : IRequest<Response<string>>
    {
        public Guid UserId { get; set; }

        // Nhóm thông tin cá nhân cơ bản (Dành cho mọi User / Manager)
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }

        // Nhóm thông tin Doanh nghiệp (Chỉ áp dụng và bắt buộc nếu là partner)
        public string? BusinessName { get; set; }
        public string? TaxCode { get; set; }
        public string? BusinessAddress { get; set; }
        public string? Position { get; set; }
    }

    public class UpdateMyProfileCommandHandler : IRequestHandler<UpdateMyProfileCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;

        public UpdateMyProfileCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response<string>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.OwnedBusinesses)
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null) throw new ApiException("Tài khoản không tồn tại.");

            // 1. Cập nhật thông tin User cơ sở
            user.FullName = request.FullName;
            user.Phone = request.Phone;
            user.Gender = request.Gender;
            user.DateOfBirth = request.DateOfBirth;
            user.UpdatedAt = DateTime.UtcNow;

            // 2. Nếu là doanh nghiệp đối tác, cập nhật luôn thông tin bảng Business
            if (user.Role.Name == "partner")
            {
                var biz = user.OwnedBusinesses.FirstOrDefault();
                if (biz != null)
                {
                    if (string.IsNullOrEmpty(request.BusinessName) || string.IsNullOrEmpty(request.TaxCode) || string.IsNullOrEmpty(request.BusinessAddress))
                    {
                        throw new ApiException("Thông tin pháp lý của doanh nghiệp không được để trống.");
                    }

                    biz.BusinessName = request.BusinessName;
                    biz.TaxCode = request.TaxCode;
                    biz.BusinessAddress = request.BusinessAddress;
                    biz.RepresentativeName = request.FullName; // Tên người đại diện đồng bộ với FullName
                    biz.Position = request.Position ?? "Owner";
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string> { Succeeded = true, Data = "Success", Message = "Cập nhật thông tin hồ sơ thành công." };
        }
    }
}
