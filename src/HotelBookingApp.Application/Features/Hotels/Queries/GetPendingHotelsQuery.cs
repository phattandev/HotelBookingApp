using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    public class GetPendingHotelsQuery : IRequest<Response<List<HotelDto>>> { }

    public class GetPendingHotelsQueryHandler : IRequestHandler<GetPendingHotelsQuery, Response<List<HotelDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetPendingHotelsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<HotelDto>>> Handle(GetPendingHotelsQuery request, CancellationToken cancellationToken)
        {
            var hotels = await _context.Hotels
                .Where(h => h.ApprovalStatus == HotelApprovalStatus.Pending)
                .OrderBy(h => h.Name) // Bỏ CreatedAt
                .Include(h => h.Business)
                .Select(h => new HotelDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    AddressLine = h.AddressLine,
                    TaxCode = h.TaxCode,
                    ApprovalStatus = h.ApprovalStatus.ToString(),
                    IsActive = h.IsActive,
                    BusinessName = h.Business.BusinessName,
                    BusinessTaxCode = h.Business.TaxCode,
                    BusinessAddress = h.Business.BusinessAddress,
                    RepresentativeName = h.Business.RepresentativeName
                })
                .ToListAsync(cancellationToken);

            return new Response<List<HotelDto>>(hotels, "Lấy danh sách chờ duyệt thành công.");
        }
    }
}
