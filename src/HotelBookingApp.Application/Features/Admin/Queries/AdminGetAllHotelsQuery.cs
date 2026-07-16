using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Queries
{
    public class AdminHotelItemDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string TaxCode { get; set; } = null!;
        public string ApprovalStatus { get; set; } = null!;
        public bool IsActive { get; set; }
        public int StarRating { get; set; }
        public string? RejectionReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessTaxCode { get; set; }
        public string? RepresentativeName { get; set; }
        public string? WardName { get; set; }
        public string? ProvinceName { get; set; }
        public int TotalRoomTypes { get; set; }
    }

    public class AdminGetAllHotelsQuery : IRequest<PaginatedResponse<AdminHotelItemDto>>
    {
        public string? Status { get; set; }   // "Pending" | "Approved" | "Rejected" | null (all)
        public bool? IsActive { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class AdminGetAllHotelsQueryHandler : IRequestHandler<AdminGetAllHotelsQuery, PaginatedResponse<AdminHotelItemDto>>
    {
        private readonly IApplicationDbContext _context;
        public AdminGetAllHotelsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<PaginatedResponse<AdminHotelItemDto>> Handle(AdminGetAllHotelsQuery request, CancellationToken cancellationToken)
        {
            var query = _context.Hotels
                .Include(h => h.Business)
                .Include(h => h.Ward)
                    .ThenInclude(w => w.Province)
                .Include(h => h.RoomTypes)
                .AsQueryable();

            // Filter by ApprovalStatus
            if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<HotelApprovalStatus>(request.Status, out var status))
                query = query.Where(h => h.ApprovalStatus == status);

            // Filter by IsActive
            if (request.IsActive.HasValue)
                query = query.Where(h => h.IsActive == request.IsActive.Value);

            // Search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var q = request.Search.ToLower();
                query = query.Where(h =>
                    h.Name.ToLower().Contains(q) ||
                    h.TaxCode.ToLower().Contains(q) ||
                    h.AddressLine.ToLower().Contains(q) ||
                    (h.Business != null && h.Business.BusinessName.ToLower().Contains(q)));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var hotels = await query
                .OrderByDescending(h => h.CreatedAt)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(h => new AdminHotelItemDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    AddressLine = h.AddressLine,
                    TaxCode = h.TaxCode,
                    ApprovalStatus = h.ApprovalStatus.ToString(),
                    IsActive = h.IsActive,
                    StarRating = h.StarRating ?? 0,
                    RejectionReason = h.RejectionReason,
                    CreatedAt = h.CreatedAt,
                    BusinessName = h.Business != null ? h.Business.BusinessName : null,
                    BusinessTaxCode = h.Business != null ? h.Business.TaxCode : null,
                    RepresentativeName = h.Business != null ? h.Business.RepresentativeName : null,
                    WardName = h.Ward != null ? h.Ward.Name : null,
                    ProvinceName = h.Ward != null && h.Ward.Province != null ? h.Ward.Province.Name : null,
                    TotalRoomTypes = h.RoomTypes.Count,
                })
                .ToListAsync(cancellationToken);

            return new PaginatedResponse<AdminHotelItemDto>(hotels, totalCount, request.Page, request.PageSize);
        }
    }
}
