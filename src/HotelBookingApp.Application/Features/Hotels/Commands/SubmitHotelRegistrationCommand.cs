using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Commands
{
    public class SubmitHotelRegistrationCommand : IRequest<Response<string>>
    {
        public Guid HotelId { get; set; }
        public Guid PartnerId { get; set; }
    }

    public class SubmitHotelRegistrationCommandHandler : IRequestHandler<SubmitHotelRegistrationCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public SubmitHotelRegistrationCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(SubmitHotelRegistrationCommand request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Không tìm thấy hồ sơ doanh nghiệp.");

            var hotel = await _context.Hotels
                .Include(h => h.Images)
                .Include(h => h.RoomTypes)
                .Include(h => h.StaffAssignments)
                .FirstOrDefaultAsync(h => h.Id == request.HotelId && h.BusinessId == business.Id, cancellationToken);

            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn hoặc bạn không có quyền thao tác.");

            if (hotel.ApprovalStatus != HotelApprovalStatus.Draft && hotel.ApprovalStatus != HotelApprovalStatus.Rejected)
                throw new ApiException("Chỉ có thể gửi đăng ký cho khách sạn đang ở trạng thái nháp (Draft) hoặc bị từ chối (Rejected).");

            // Validate requirements
            var errors = new List<string>();

            if (!hotel.Images.Any())
                errors.Add("Khách sạn phải có ít nhất 1 hình ảnh.");

            if (!hotel.RoomTypes.Any())
                errors.Add("Khách sạn phải có ít nhất 1 loại phòng.");

            if (!hotel.StaffAssignments.Any(sa => sa.RoleInHotel == "manager" && sa.IsActive))
                errors.Add("Khách sạn phải có ít nhất 1 quản lý (manager) được phân công.");

            if (errors.Any())
            {
                var errorMsg = "Không đủ điều kiện gửi đăng ký:\n- " + string.Join("\n- ", errors);
                throw new ApiException(errorMsg);
            }

            hotel.ApprovalStatus = HotelApprovalStatus.Pending;
            hotel.RejectionReason = null; // Clear rejection reason if any
            hotel.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string>("Đã gửi đơn đăng ký khách sạn thành công. Vui lòng chờ Admin phê duyệt.");
        }
    }
}
