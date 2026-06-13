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

            var employees = await _context.Users
                .Include(u => u.Role)
                .Where(u => u.BusinessId == business.Id)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new EmployeeDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    Phone = u.Phone,
                    Role = u.Role.Name,
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new Response<List<EmployeeDto>>(employees, "Lấy danh sách nhân sự thành công.");
        }
    }
}
