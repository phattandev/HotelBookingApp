using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.BusinessStaff.Commands
{
    public class ToggleEmployeeStatusCommand : IRequest<Response<string>>
    {
        public Guid PartnerId { get; set; }
        public Guid EmployeeId { get; set; }
    }

    public class ToggleEmployeeStatusCommandHandler : IRequestHandler<ToggleEmployeeStatusCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public ToggleEmployeeStatusCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(ToggleEmployeeStatusCommand request, CancellationToken cancellationToken)
        {
            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.OwnerId == request.PartnerId, cancellationToken);
            if (business == null) throw new ApiException("Hồ sơ doanh nghiệp không tồn tại.");

            var staff = await _context.BusinessStaff.Include(bs => bs.User).FirstOrDefaultAsync(bs => bs.UserId == request.EmployeeId && bs.BusinessId == business.Id, cancellationToken);
            if (staff == null) throw new ApiException("Không tìm thấy nhân viên thuộc doanh nghiệp này.");

            staff.User.IsActive = !staff.User.IsActive; // Đảo trạng thái
            staff.User.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            string statusText = staff.User.IsActive ? "kích hoạt" : "vô hiệu hóa";
            return new Response<string>($"Đã {statusText} tài khoản nhân viên.");
        }
    }
}
