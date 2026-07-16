using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Queries
{
    public class AdminUserItemDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string RoleName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        // For partner/business owner
        public string? BusinessName { get; set; }
        public string? BusinessStatus { get; set; }
        // For staff
        public string? AssignedHotelName { get; set; }
    }

    public class AdminGetAllUsersQuery : IRequest<PaginatedResponse<AdminUserItemDto>>
    {
        public string? Role { get; set; }          // "customer" | "partner" | "manager" | "staff" | "admin"
        public bool? IsActive { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class AdminGetAllUsersQueryHandler : IRequestHandler<AdminGetAllUsersQuery, PaginatedResponse<AdminUserItemDto>>
    {
        private readonly IApplicationDbContext _context;
        public AdminGetAllUsersQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<PaginatedResponse<AdminUserItemDto>> Handle(AdminGetAllUsersQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.OwnedBusinesses)
                .Include(u => u.StaffAssignments)
                    .ThenInclude(a => a.Hotel)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Role))
                query = query.Where(u => u.Role != null && u.Role.Name.ToLower() == request.Role.ToLower());

            if (request.IsActive.HasValue)
                query = query.Where(u => u.IsActive == request.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var q = request.Search.ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(q) ||
                    u.Email.ToLower().Contains(q) ||
                    u.FullName.ToLower().Contains(q) ||
                    (u.Phone != null && u.Phone.Contains(q)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var dtos = users.Select(u =>
            {
                var business = u.OwnedBusinesses.FirstOrDefault();
                var assignment = u.StaffAssignments.FirstOrDefault(a => a.IsActive);
                return new AdminUserItemDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    FullName = u.FullName,
                    Phone = u.Phone,
                    RoleName = u.Role?.Name ?? "N/A",
                    IsActive = u.IsActive,
                    CreatedAt = u.CreatedAt,
                    BusinessName = business?.BusinessName,
                    BusinessStatus = business?.VerificationStatus.ToString(),
                    AssignedHotelName = assignment?.Hotel?.Name,
                };
            }).ToList();

            return new PaginatedResponse<AdminUserItemDto>(dtos, totalCount, request.Page, request.PageSize);
        }
    }
}
