using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.StaffAssignment.Commands
{
    public class AssignStaffCommand : IRequest<Response<string>>
    {
        public Guid PartnerId { get; set; }  // Gán từ JWT Token qua Controller
        public Guid EmployeeId { get; set; }
        public Guid HotelId { get; set; }
        /// <summary>
        /// Vai trò tại khách sạn: "manager" (Quản lý) hoặc "staff" (Nhân viên).
        /// </summary>
        public string RoleInHotel { get; set; } = null!;
    }

    public class AssignStaffCommandHandler : IRequestHandler<AssignStaffCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public AssignStaffCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(AssignStaffCommand request, CancellationToken cancellationToken)
        {
            var roleNormalized = request.RoleInHotel.ToLower();

            // 1. Lấy doanh nghiệp của partner
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Hồ sơ doanh nghiệp không tồn tại.");

            // 2. Kiểm tra employee thuộc doanh nghiệp này
            var businessStaff = await _context.BusinessStaff
                .Include(bs => bs.User)
                .ThenInclude(u => u.Role)
                .FirstOrDefaultAsync(bs => bs.UserId == request.EmployeeId && bs.BusinessId == business.Id, cancellationToken);
            if (businessStaff == null) throw new ApiException("Không tìm thấy nhân viên thuộc doanh nghiệp này.");

            var employee = businessStaff.User;

            // 3. Kiểm tra hotel thuộc doanh nghiệp này
            var hotel = await _context.Hotels
                .FirstOrDefaultAsync(h => h.Id == request.HotelId && h.BusinessId == business.Id, cancellationToken);
            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn thuộc doanh nghiệp này.");
            if (!hotel.IsActive) throw new ApiException("Khách sạn này chưa được phê duyệt hoặc đã bị vô hiệu hóa.");

            // 4. [ANTI-ANOMALY] Đảm bảo nhân viên và khách sạn cùng thuộc một doanh nghiệp
            if (businessStaff.BusinessId != hotel.BusinessId)
                throw new ApiException("Không thể phân công nhân viên vào khách sạn thuộc doanh nghiệp khác.");

            // 5. [BUSINESS RULE] Mỗi khách sạn chỉ được có tối đa 1 quản lý (manager) tại một thời điểm
            if (roleNormalized == "manager")
            {
                var existingManager = await _context.HotelStaffAssignments
                    .FirstOrDefaultAsync(a => a.HotelId == request.HotelId
                                           && a.RoleInHotel == "manager"
                                           && a.IsActive
                                           && a.UserId != request.EmployeeId, // không tính chính nhân viên này
                                          cancellationToken);
                if (existingManager != null)
                    throw new ApiException(
                        "Khách sạn này đã có quản lý đang hoạt động. " +
                        "Vui lòng thu hồi quyền quản lý của người hiện tại trước khi phân công mới.");
            }

            // 6. Hủy phân công cũ của nhân viên này (nếu có) — mỗi nhân viên chỉ ở 1 khách sạn tại 1 thời điểm
            var existingAssignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.EmployeeId && a.IsActive, cancellationToken);
            if (existingAssignment != null)
            {
                existingAssignment.IsActive = false;
            }

            // 7. Tạo phân công mới
            var newAssignment = new HotelStaffAssignment
            {
                Id = Guid.NewGuid(),
                UserId = request.EmployeeId,
                HotelId = request.HotelId,
                RoleInHotel = roleNormalized,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.HotelStaffAssignments.Add(newAssignment);

            // 8. Cập nhật Role hệ thống của User để JWT phản ánh đúng quyền
            // manager -> role "manager" | staff -> role "staff"
            var targetRoleName = roleNormalized == "manager" ? "manager" : "staff";
            var targetRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == targetRoleName, cancellationToken);
            if (targetRole == null)
                throw new ApiException($"Role '{targetRoleName}' chưa được cấu hình. Vui lòng liên hệ Admin.");

            employee.RoleId = targetRole.Id;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            var roleLabel = roleNormalized == "manager" ? "Quản lý" : "Nhân viên";
            return new Response<string>(
                $"Đã phân công '{employee.FullName}' làm {roleLabel} tại '{hotel.Name}'.");
        }
    }

    public class AssignStaffCommandValidator : AbstractValidator<AssignStaffCommand>
    {
        // Hệ thống chỉ có 2 vai trò tại khách sạn: quản lý và nhân viên
        private static readonly string[] ValidRoles = { "manager", "staff" };

        public AssignStaffCommandValidator()
        {
            RuleFor(x => x.EmployeeId)
                .NotEmpty().WithMessage("Nhân viên không được để trống.");

            RuleFor(x => x.HotelId)
                .NotEmpty().WithMessage("Khách sạn không được để trống.");

            RuleFor(x => x.RoleInHotel)
                .NotEmpty().WithMessage("Vai trò tại khách sạn không được để trống.")
                .Must(r => ValidRoles.Contains(r.ToLower()))
                .WithMessage("Vai trò chỉ chấp nhận 'manager' (Quản lý) hoặc 'staff' (Nhân viên).");
        }
    }
}
