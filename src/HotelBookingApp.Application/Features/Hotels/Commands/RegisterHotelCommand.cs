using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Commands
{
    public class RegisterHotelCommand : IRequest<Response<Guid>>
    {
        public Guid PartnerId { get; set; }
        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string TaxCode { get; set; } = null!;
        public Guid WardId { get; set; }
    }

    public class RegisterHotelCommandHandler : IRequestHandler<RegisterHotelCommand, Response<Guid>>
    {
        private readonly IApplicationDbContext _context;
        public RegisterHotelCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<Guid>> Handle(RegisterHotelCommand request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Không tìm thấy hồ sơ doanh nghiệp hợp lệ.");

            var hotel = new Hotel
            {
                Id = Guid.NewGuid(),
                BusinessId = business.Id,
                Name = request.Name,
                AddressLine = request.AddressLine,
                WardId = request.WardId,
                TaxCode = request.TaxCode,
                ApprovalStatus = HotelApprovalStatus.Pending,
                IsActive = false
            };

            _context.Hotels.Add(hotel);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<Guid>(hotel.Id, "Đăng ký khách sạn thành công, vui lòng chờ Admin phê duyệt.");
        }
    }
}
