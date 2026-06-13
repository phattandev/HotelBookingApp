using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    public class GetMyHotelsQuery : IRequest<Response<List<HotelDto>>>
    {
        public Guid PartnerId { get; set; }
    }

    public class GetMyHotelsQueryHandler : IRequestHandler<GetMyHotelsQuery, Response<List<HotelDto>>>
    {
        private readonly IApplicationDbContext _context;
        public GetMyHotelsQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<HotelDto>>> Handle(GetMyHotelsQuery request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Không tìm thấy hồ sơ doanh nghiệp.");

            var hotels = await _context.Hotels
                .Where(h => h.BusinessId == business.Id)
                .OrderBy(h => h.Name) // Bỏ CreatedAt
                .Select(h => new HotelDto
                {
                    Id = h.Id,
                    Name = h.Name,
                    AddressLine = h.AddressLine,
                    TaxCode = h.TaxCode,
                    ApprovalStatus = h.ApprovalStatus.ToString(),
                    IsActive = h.IsActive
                })
                .ToListAsync(cancellationToken);

            return new Response<List<HotelDto>>(hotels, "Lấy danh sách khách sạn thành công.");
        }
    }
}
