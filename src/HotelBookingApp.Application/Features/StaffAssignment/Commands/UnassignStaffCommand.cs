using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.StaffAssignment.Commands
{
    public class UnassignStaffCommand : IRequest<Response<string>>
    {
        public Guid PartnerId { get; set; }  // Gán từ JWT Token qua Controller
        public Guid EmployeeId { get; set; }
    }

    public class UnassignStaffCommandHandler : IRequestHandler<UnassignStaffCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public UnassignStaffCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(UnassignStaffCommand request, CancellationToken cancellationToken)
        {
            // 1. Lấy doanh nghiệp của partner
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Hồ sơ doanh nghiệp không tồn tại.");

            // 2. Kiểm tra employee thuộc doanh nghiệp
            var businessStaff = await _context.BusinessStaff
                .Include(bs => bs.User)
                .FirstOrDefaultAsync(bs => bs.UserId == request.EmployeeId && bs.BusinessId == business.Id, cancellationToken);
            if (businessStaff == null) throw new ApiException("Không tìm thấy nhân viên thuộc doanh nghiệp này.");

            var employee = businessStaff.User;

            // 3. Hủy phân công đang active
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.EmployeeId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Nhân viên này chưa được phân công.");

            assignment.IsActive = false;

            // 4. Trả role hệ thống về "staff"
            var staffRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "staff", cancellationToken);
            if (staffRole == null) throw new ApiException("Role 'staff' chưa được cấu hình. Vui lòng liên hệ Admin.");

            employee.RoleId = staffRole.Id;
            employee.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string>($"Đã hủy phân công của '{employee.FullName}'. Tài khoản trở về trạng thái 'Chưa phân công'.");
        }
    }
}
