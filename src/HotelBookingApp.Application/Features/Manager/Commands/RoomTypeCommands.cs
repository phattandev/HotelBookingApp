using FluentValidation;
using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Commands
{
    // ====== CREATE ======
    public class CreateRoomTypeCommand : IRequest<Response<Guid>>
    {
        public Guid ManagerId { get; set; }
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        public int TotalRooms { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<Guid> AmenityIds { get; set; } = new();
    }

    public class CreateRoomTypeCommandHandler : IRequestHandler<CreateRoomTypeCommand, Response<Guid>>
    {
        private readonly IApplicationDbContext _context;
        public CreateRoomTypeCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<Guid>> Handle(CreateRoomTypeCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var roomType = new RoomType
            {
                Id = Guid.NewGuid(),
                HotelId = assignment.HotelId,
                Name = request.Name,
                BasePrice = request.BasePrice,
                MaxAdults = request.MaxAdults,
                MaxChildren = request.MaxChildren,
                TotalRooms = request.TotalRooms,
                Description = request.Description,
                IsActive = true
            };

            _context.RoomTypes.Add(roomType);

            // Thêm tiện nghi cho loại phòng
            if (request.AmenityIds.Any())
            {
                var amenities = request.AmenityIds.Select(id => new RoomTypeAmenity
                {
                    RoomTypeId = roomType.Id,
                    AmenityId = id
                });
                await _context.RoomTypeAmenities.AddRangeAsync(amenities, cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);
            return new Response<Guid>(roomType.Id, $"Đã tạo loại phòng '{request.Name}' thành công.");
        }
    }

    public class CreateRoomTypeCommandValidator : AbstractValidator<CreateRoomTypeCommand>
    {
        public CreateRoomTypeCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên loại phòng không được để trống.")
                .MaximumLength(150).WithMessage("Tên tối đa 150 ký tự.");
            RuleFor(x => x.BasePrice).GreaterThan(0).WithMessage("Giá cơ bản phải lớn hơn 0.");
            RuleFor(x => x.MaxAdults).GreaterThanOrEqualTo(1).WithMessage("Sức chứa người lớn ít nhất 1.");
            RuleFor(x => x.TotalRooms).GreaterThanOrEqualTo(1).WithMessage("Số lượng phòng ít nhất 1.");
        }
    }

    // ====== UPDATE ======
    public class UpdateRoomTypeCommand : IRequest<Response<string>>
    {
        public Guid ManagerId { get; set; }
        public Guid RoomTypeId { get; set; }
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        public int TotalRooms { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<Guid> AmenityIds { get; set; } = new(); // Danh sách IDs muốn giữ lại (sync)
    }

    public class UpdateRoomTypeCommandHandler : IRequestHandler<UpdateRoomTypeCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public UpdateRoomTypeCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(UpdateRoomTypeCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var roomType = await _context.RoomTypes
                .FirstOrDefaultAsync(rt => rt.Id == request.RoomTypeId && rt.HotelId == assignment.HotelId, cancellationToken);
            if (roomType == null) throw new ApiException("Không tìm thấy loại phòng thuộc khách sạn này.");

            roomType.Name = request.Name;
            roomType.BasePrice = request.BasePrice;
            roomType.MaxAdults = request.MaxAdults;
            roomType.MaxChildren = request.MaxChildren;
            roomType.TotalRooms = request.TotalRooms;
            roomType.Description = request.Description;

            // Sync tiện nghi
            var existingAmenities = await _context.RoomTypeAmenities
                .Where(ra => ra.RoomTypeId == request.RoomTypeId)
                .ToListAsync(cancellationToken);

            _context.RoomTypeAmenities.RemoveRange(existingAmenities);

            var newAmenities = request.AmenityIds.Select(id => new RoomTypeAmenity
            {
                RoomTypeId = request.RoomTypeId,
                AmenityId = id
            });
            await _context.RoomTypeAmenities.AddRangeAsync(newAmenities, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string> { Succeeded = true, Data = "Success", Message = $"Đã cập nhật loại phòng '{roomType.Name}' thành công." };
        }
    }

    public class UpdateRoomTypeCommandValidator : AbstractValidator<UpdateRoomTypeCommand>
    {
        public UpdateRoomTypeCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Tên loại phòng không được để trống.")
                .MaximumLength(150).WithMessage("Tên tối đa 150 ký tự.");
            RuleFor(x => x.BasePrice).GreaterThan(0).WithMessage("Giá cơ bản phải lớn hơn 0.");
            RuleFor(x => x.MaxAdults).GreaterThanOrEqualTo(1).WithMessage("Sức chứa người lớn ít nhất 1.");
            RuleFor(x => x.TotalRooms).GreaterThanOrEqualTo(1).WithMessage("Số lượng phòng ít nhất 1.");
        }
    }

    // ====== DELETE (Soft Delete) ======
    public class DeleteRoomTypeCommand : IRequest<Response<string>>
    {
        public Guid ManagerId { get; set; }
        public Guid RoomTypeId { get; set; }
    }

    public class DeleteRoomTypeCommandHandler : IRequestHandler<DeleteRoomTypeCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public DeleteRoomTypeCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(DeleteRoomTypeCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var roomType = await _context.RoomTypes
                .FirstOrDefaultAsync(rt => rt.Id == request.RoomTypeId && rt.HotelId == assignment.HotelId, cancellationToken);
            if (roomType == null) throw new ApiException("Không tìm thấy loại phòng thuộc khách sạn này.");

            roomType.IsActive = false; // Soft delete
            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string> { Succeeded = true, Data = "Success", Message = $"Đã xóa loại phòng '{roomType.Name}'." };
        }
    }

    // ====== RESTORE ======
    public class RestoreRoomTypeCommand : IRequest<Response<string>>
    {
        public Guid ManagerId { get; set; }
        public Guid RoomTypeId { get; set; }
    }

    public class RestoreRoomTypeCommandHandler : IRequestHandler<RestoreRoomTypeCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public RestoreRoomTypeCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(RestoreRoomTypeCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var roomType = await _context.RoomTypes
                .FirstOrDefaultAsync(rt => rt.Id == request.RoomTypeId && rt.HotelId == assignment.HotelId, cancellationToken);
            if (roomType == null) throw new ApiException("Không tìm thấy loại phòng thuộc khách sạn này.");

            roomType.IsActive = true; // Restore
            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string> { Succeeded = true, Data = "Success", Message = $"Đã mở lại loại phòng '{roomType.Name}'." };
        }
    }
}
