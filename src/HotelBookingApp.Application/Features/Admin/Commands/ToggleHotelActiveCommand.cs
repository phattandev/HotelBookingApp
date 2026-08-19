using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Commands
{
    /// <summary>
    /// Bật/tắt trạng thái hoạt động của khách sạn (Suspend/Unsuspend).
    /// Chỉ áp dụng cho khách sạn đã được duyệt (ApprovalStatus = Approved).
    /// </summary>
    public class ToggleHotelActiveCommand : IRequest<Response<string>>
    {
        public Guid HotelId { get; set; }
    }

    public class ToggleHotelActiveCommandHandler : IRequestHandler<ToggleHotelActiveCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public ToggleHotelActiveCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(ToggleHotelActiveCommand request, CancellationToken cancellationToken)
        {
            var hotel = await _context.Hotels.FindAsync(new object[] { request.HotelId }, cancellationToken);
            if (hotel == null)
                throw new ApiException("Không tìm thấy khách sạn.");

            hotel.IsActive = !hotel.IsActive;
            hotel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var status = hotel.IsActive ? "kích hoạt" : "đình chỉ";
            return new Response<string> { Succeeded = true, Data = "Success", Message = $"Đã {status} khách sạn '{hotel.Name}' thành công." };
        }
    }
}
