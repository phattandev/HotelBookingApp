using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Admin.Commands
{
    /// <summary>
    /// Bật/tắt trạng thái hoạt động của tài khoản người dùng (khóa/mở khóa).
    /// Admin không thể tự khóa chính mình.
    /// </summary>
    public class ToggleUserStatusCommand : IRequest<Response<string>>
    {
        public Guid TargetUserId { get; set; }
        public Guid AdminId { get; set; }   // để tránh tự khóa chính mình
    }

    public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public ToggleUserStatusCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
        {
            if (request.TargetUserId == request.AdminId)
                throw new ApiException("Bạn không thể khóa tài khoản của chính mình.");

            var user = await _context.Users.FindAsync(new object[] { request.TargetUserId }, cancellationToken);
            if (user == null)
                throw new ApiException("Không tìm thấy tài khoản.");

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            var status = user.IsActive ? "mở khóa" : "khóa";
            return new Response<string>($"Đã {status} tài khoản '{user.Username}' thành công.");
        }
    }
}
