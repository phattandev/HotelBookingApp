using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Commands
{
    public class UpdateHotelBasicInfoCommand : IRequest<Response<string>>
    {
        public Guid HotelId { get; set; }
        public Guid UserId { get; set; } // Partner or Manager
        public string Role { get; set; } = null!; // "partner" or "manager"

        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public Guid WardId { get; set; }
    }

    public class UpdateHotelBasicInfoCommandHandler : IRequestHandler<UpdateHotelBasicInfoCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public UpdateHotelBasicInfoCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(UpdateHotelBasicInfoCommand request, CancellationToken cancellationToken)
        {
            var hotel = await _context.Hotels
                .Include(h => h.Business)
                .Include(h => h.StaffAssignments)
                .FirstOrDefaultAsync(h => h.Id == request.HotelId, cancellationToken);

            if (hotel == null) throw new ApiException("Không tìm thấy khách sạn.");

            // Kiểm tra quyền
            if (request.Role == "partner")
            {
                if (hotel.Business.OwnerId != request.UserId)
                    throw new ApiException("Bạn không có quyền chỉnh sửa khách sạn này.");
            }
            else if (request.Role == "manager")
            {
                var isManager = hotel.StaffAssignments.Any(sa => sa.UserId == request.UserId && sa.RoleInHotel == "manager" && sa.IsActive);
                if (!isManager)
                    throw new ApiException("Bạn không có quyền quản lý khách sạn này.");
            }
            else
            {
                throw new ApiException("Quyền không hợp lệ.");
            }

            // Kiểm tra trạng thái: Không cho phép sửa nếu Pending
            // Requirement: Manager (and Partner) allowed to edit all states except Pending.
            if (hotel.ApprovalStatus == HotelApprovalStatus.Pending)
            {
                throw new ApiException("Không thể chỉnh sửa thông tin khi khách sạn đang chờ duyệt (Pending).");
            }

            // Kiểm tra ward tồn tại
            var wardExists = await _context.Wards.AnyAsync(w => w.Id == request.WardId, cancellationToken);
            if (!wardExists)
                throw new ApiException("Địa chỉ phường/xã không hợp lệ.");

            hotel.Name = request.Name;
            hotel.AddressLine = request.AddressLine;
            hotel.WardId = request.WardId;
            hotel.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return new Response<string>("Cập nhật thông tin khách sạn thành công.");
        }
    }

    public class UpdateHotelBasicInfoCommandValidator : AbstractValidator<UpdateHotelBasicInfoCommand>
    {
        public UpdateHotelBasicInfoCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên khách sạn không được để trống.")
                .MaximumLength(200).WithMessage("Tên khách sạn tối đa 200 ký tự.");

            RuleFor(x => x.AddressLine)
                .NotEmpty().WithMessage("Địa chỉ khách sạn không được để trống.")
                .MaximumLength(255).WithMessage("Địa chỉ tối đa 255 ký tự.");

            RuleFor(x => x.WardId)
                .NotEmpty().WithMessage("Vui lòng chọn phường/xã cho địa chỉ khách sạn.");
        }
    }
}
