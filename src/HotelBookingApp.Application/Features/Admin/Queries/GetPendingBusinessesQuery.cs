using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using HotelBookingApp.Application.Common.Interfaces;

namespace HotelBookingApp.Application.Features.Admin.Queries
{
    public class BusinessApprovalDto
    {
        public Guid Id { get; set; }
        public Guid OwnerId { get; set; }
        public string BusinessName { get; set; } = null!;
        public string TaxCode { get; set; } = null!;
        public string BusinessAddress { get; set; } = null!;
        public string RepresentativeName { get; set; } = null!;
        public string Position { get; set; } = null!;
        public string VerificationStatus { get; set; } = null!;
        public string OwnerEmail { get; set; } = null!;
        public string OwnerUsername { get; set; } = null!;
        public string OwnerPhone { get; set; } = null!;
    }

    public class GetPendingBusinessesQuery : IRequest<Response<List<BusinessApprovalDto>>>
    {
    }

    public class GetPendingBusinessesQueryHandler : IRequestHandler<GetPendingBusinessesQuery, Response<List<BusinessApprovalDto>>>
    {
        private readonly IApplicationDbContext _context;

        public GetPendingBusinessesQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Response<List<BusinessApprovalDto>>> Handle(GetPendingBusinessesQuery request, CancellationToken cancellationToken)
        {
            var businesses = await _context.Businesses
                .Include(b => b.Owner)
                .Where(b => b.VerificationStatus == BusinessVerificationStatus.Pending)
                .Select(b => new BusinessApprovalDto
                {
                    Id = b.Id,
                    OwnerId = b.OwnerId,
                    BusinessName = b.BusinessName,
                    TaxCode = b.TaxCode,
                    BusinessAddress = b.BusinessAddress,
                    RepresentativeName = b.RepresentativeName,
                    Position = b.Position,
                    VerificationStatus = b.VerificationStatus.ToString(),
                    OwnerEmail = b.Owner.Email,
                    OwnerUsername = b.Owner.Username,
                    OwnerPhone = b.Owner.Phone ?? ""
                })
                .ToListAsync(cancellationToken);

            return new Response<List<BusinessApprovalDto>>(businesses, "Lấy danh sách doanh nghiệp chờ duyệt thành công.");
        }
    }
}
