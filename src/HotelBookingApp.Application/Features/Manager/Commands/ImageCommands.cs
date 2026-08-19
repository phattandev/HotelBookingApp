using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.DTOs.HotelDto;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Commands
{
    // ====== UPLOAD ẢNH KHÁCH SẠN ======
    public class UploadHotelImageCommand : IRequest<Response<HotelImageDto>>
    {
        public Guid ManagerId { get; set; }
        public Stream ImageStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public bool SetAsPrimary { get; set; } = false;
    }

    public class UploadHotelImageCommandHandler : IRequestHandler<UploadHotelImageCommand, Response<HotelImageDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        public UploadHotelImageCommandHandler(IApplicationDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Response<HotelImageDto>> Handle(UploadHotelImageCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var hotelId = assignment.HotelId;

            // Upload lên Cloudinary
            var uploadResult = await _cloudinary.UploadImageAsync(
                request.ImageStream, request.FileName,
                $"hotels/{hotelId}");

            // Nếu set làm ảnh chính, bỏ ảnh chính cũ
            if (request.SetAsPrimary)
            {
                var currentPrimary = await _context.HotelImages
                    .Where(i => i.HotelId == hotelId && i.IsPrimary)
                    .ToListAsync(cancellationToken);
                currentPrimary.ForEach(i => i.IsPrimary = false);
            }

            // Kiểm tra nếu là ảnh đầu tiên, tự động set primary
            var imageCount = await _context.HotelImages.CountAsync(i => i.HotelId == hotelId, cancellationToken);
            var isPrimary = request.SetAsPrimary || imageCount == 0;

            var image = new HotelImage
            {
                Id = Guid.NewGuid(),
                HotelId = hotelId,
                Url = uploadResult.Url,
                PublicId = uploadResult.PublicId,
                IsPrimary = isPrimary,
                DisplayOrder = imageCount,
                CreatedAt = DateTime.UtcNow
            };

            _context.HotelImages.Add(image);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<HotelImageDto>(new HotelImageDto
            {
                Id = image.Id,
                Url = image.Url,
                PublicId = image.PublicId,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder
            }, "Upload ảnh khách sạn thành công.");
        }
    }

    // ====== XÓA ẢNH KHÁCH SẠN ======
    public class DeleteHotelImageCommand : IRequest<Response<string>>
    {
        public Guid ManagerId { get; set; }
        public Guid ImageId { get; set; }
    }

    public class DeleteHotelImageCommandHandler : IRequestHandler<DeleteHotelImageCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        public DeleteHotelImageCommandHandler(IApplicationDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Response<string>> Handle(DeleteHotelImageCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var image = await _context.HotelImages
                .FirstOrDefaultAsync(i => i.Id == request.ImageId && i.HotelId == assignment.HotelId, cancellationToken);
            if (image == null) throw new ApiException("Không tìm thấy ảnh.");

            // Xóa trên Cloudinary
            await _cloudinary.DeleteImageAsync(image.PublicId);

            // Nếu xóa ảnh primary, tự set ảnh kế tiếp làm primary
            if (image.IsPrimary)
            {
                var nextImage = await _context.HotelImages
                    .Where(i => i.HotelId == assignment.HotelId && i.Id != request.ImageId)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefaultAsync(cancellationToken);
                if (nextImage != null) nextImage.IsPrimary = true;
            }

            _context.HotelImages.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string> { Succeeded = true, Data = "Success", Message = "Đã xóa ảnh thành công." };
        }
    }

    // ====== UPLOAD ẢNH PHÒNG ======
    public class UploadRoomTypeImageCommand : IRequest<Response<HotelImageDto>>
    {
        public Guid ManagerId { get; set; }
        public Guid RoomTypeId { get; set; }
        public Stream ImageStream { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public bool SetAsPrimary { get; set; } = false;
    }

    public class UploadRoomTypeImageCommandHandler : IRequestHandler<UploadRoomTypeImageCommand, Response<HotelImageDto>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        public UploadRoomTypeImageCommandHandler(IApplicationDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Response<HotelImageDto>> Handle(UploadRoomTypeImageCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var roomType = await _context.RoomTypes
                .FirstOrDefaultAsync(rt => rt.Id == request.RoomTypeId && rt.HotelId == assignment.HotelId, cancellationToken);
            if (roomType == null) throw new ApiException("Không tìm thấy loại phòng thuộc khách sạn này.");

            var uploadResult = await _cloudinary.UploadImageAsync(
                request.ImageStream, request.FileName,
                $"hotels/{assignment.HotelId}/rooms/{request.RoomTypeId}");

            if (request.SetAsPrimary)
            {
                var currentPrimary = await _context.RoomTypeImages
                    .Where(i => i.RoomTypeId == request.RoomTypeId && i.IsPrimary)
                    .ToListAsync(cancellationToken);
                currentPrimary.ForEach(i => i.IsPrimary = false);
            }

            var imageCount = await _context.RoomTypeImages.CountAsync(i => i.RoomTypeId == request.RoomTypeId, cancellationToken);
            var image = new RoomTypeImage
            {
                Id = Guid.NewGuid(),
                RoomTypeId = request.RoomTypeId,
                Url = uploadResult.Url,
                PublicId = uploadResult.PublicId,
                IsPrimary = request.SetAsPrimary || imageCount == 0,
                DisplayOrder = imageCount,
                CreatedAt = DateTime.UtcNow
            };

            _context.RoomTypeImages.Add(image);
            await _context.SaveChangesAsync(cancellationToken);

            return new Response<HotelImageDto>(new HotelImageDto
            {
                Id = image.Id,
                Url = image.Url,
                PublicId = image.PublicId,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder
            }, "Upload ảnh loại phòng thành công.");
        }
    }

    // ====== XÓA ẢNH PHÒNG ======
    public class DeleteRoomTypeImageCommand : IRequest<Response<string>>
    {
        public Guid ManagerId { get; set; }
        public Guid ImageId { get; set; }
    }

    public class DeleteRoomTypeImageCommandHandler : IRequestHandler<DeleteRoomTypeImageCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        public DeleteRoomTypeImageCommandHandler(IApplicationDbContext context, ICloudinaryService cloudinary)
        {
            _context = context;
            _cloudinary = cloudinary;
        }

        public async Task<Response<string>> Handle(DeleteRoomTypeImageCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var image = await _context.RoomTypeImages
                .Include(i => i.RoomType)
                .FirstOrDefaultAsync(i => i.Id == request.ImageId && i.RoomType.HotelId == assignment.HotelId, cancellationToken);
            if (image == null) throw new ApiException("Không tìm thấy ảnh.");

            await _cloudinary.DeleteImageAsync(image.PublicId);

            if (image.IsPrimary)
            {
                var nextImage = await _context.RoomTypeImages
                    .Where(i => i.RoomTypeId == image.RoomTypeId && i.Id != request.ImageId)
                    .OrderBy(i => i.DisplayOrder)
                    .FirstOrDefaultAsync(cancellationToken);
                if (nextImage != null) nextImage.IsPrimary = true;
            }

            _context.RoomTypeImages.Remove(image);
            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string> { Succeeded = true, Data = "Success", Message = "Đã xóa ảnh thành công." };
        }
    }
}
