using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.BusinessDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.StaffAssignment.Queries
{
    public class GetStaffAssignmentsQuery : IRequest<Response<List<StaffAssignmentDto>>>
    {
        public Guid PartnerId { get; set; }
        public GetStaffAssignmentsQuery(Guid partnerId) => PartnerId = partnerId;
    }

    public class GetStaffAssignmentsQueryHandler : IRequestHandler<GetStaffAssignmentsQuery, Response<List<StaffAssignmentDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetStaffAssignmentsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<StaffAssignmentDto>>> Handle(GetStaffAssignmentsQuery request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Hồ sơ doanh nghiệp không tồn tại.");

            // Lấy tất cả phân công đang active của doanh nghiệp này
            var assignments = await _context.HotelStaffAssignments
                .Include(a => a.User)
                .Include(a => a.Hotel)
                .Where(a => a.IsActive && a.Hotel.BusinessId == business.Id)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new StaffAssignmentDto
                {
                    AssignmentId = a.Id,
                    EmployeeId = a.UserId,
                    EmployeeFullName = a.User.FullName,
                    EmployeeEmail = a.User.Email,
                    HotelId = a.HotelId,
                    HotelName = a.Hotel.Name,
                    RoleInHotel = a.RoleInHotel,
                    AssignedAt = a.AssignedAt,
                    IsActive = a.IsActive
                })
                .ToListAsync(cancellationToken);

            return new Response<List<StaffAssignmentDto>>(assignments, "Lấy danh sách phân công thành công.");
        }
    }
}
