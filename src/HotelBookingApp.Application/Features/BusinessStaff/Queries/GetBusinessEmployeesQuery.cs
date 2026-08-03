using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.BusinessDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.BusinessStaff.Queries
{
    public class GetBusinessEmployeesQuery : IRequest<Response<List<EmployeeDto>>>
    {
        public Guid PartnerId { get; set; }
        public GetBusinessEmployeesQuery(Guid partnerId) => PartnerId = partnerId;
    }

    public class GetBusinessEmployeesQueryHandler : IRequestHandler<GetBusinessEmployeesQuery, Response<List<EmployeeDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetBusinessEmployeesQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<EmployeeDto>>> Handle(GetBusinessEmployeesQuery request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Bạn chưa sở hữu hồ sơ doanh nghiệp nào.");

            // LEFT JOIN với HotelStaffAssignment để lấy thông tin phân công hiện tại (IsActive = true)
            var employees = await _context.BusinessStaff
                .Include(bs => bs.User)
                .ThenInclude(u => u.Role)
                .Where(bs => bs.BusinessId == business.Id)
                .OrderByDescending(bs => bs.CreatedAt)
                .Select(bs => new EmployeeDto
                {
                    Id = bs.User.Id,
                    Email = bs.User.Email,
                    Username = bs.User.Username,
                    FullName = bs.User.FullName,
                    Phone = bs.User.Phone,
                    Role = bs.User.Role.Name,
                    IsActive = bs.User.IsActive,
                    CreatedAt = bs.User.CreatedAt,
                    // Lấy phân công đang active (nếu có)
                    AssignedHotelId = bs.User.StaffAssignments
                        .Where(a => a.IsActive)
                        .Select(a => (Guid?)a.HotelId)
                        .FirstOrDefault(),
                    AssignedHotelName = bs.User.StaffAssignments
                        .Where(a => a.IsActive)
                        .Select(a => a.Hotel.Name)
                        .FirstOrDefault(),
                    RoleInHotel = bs.User.StaffAssignments
                        .Where(a => a.IsActive)
                        .Select(a => a.RoleInHotel)
                        .FirstOrDefault(),
                    Title = "Nhan vien"
                })
                .ToListAsync(cancellationToken);

            return new Response<List<EmployeeDto>>(employees, "Lấy danh sách nhân sự thành công.");
        }
    }
}
