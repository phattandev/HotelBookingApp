using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Commands
{
    /// <summary>Đồng bộ tiện nghi cho khách sạn: nhận danh sách amenityIds muốn giữ lại, xóa phần còn lại.</summary>
    public class SyncHotelAmenitiesCommand : IRequest<Response<string>>
    {
        public Guid ManagerId { get; set; }
        public List<Guid> AmenityIds { get; set; } = new(); // Danh sách IDs muốn GIỮ LẠI
    }

    public class SyncHotelAmenitiesCommandHandler : IRequestHandler<SyncHotelAmenitiesCommand, Response<string>>
    {
        private readonly IApplicationDbContext _context;
        public SyncHotelAmenitiesCommandHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<string>> Handle(SyncHotelAmenitiesCommand request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a => a.UserId == request.ManagerId && a.IsActive, cancellationToken);
            if (assignment == null) throw new ApiException("Bạn chưa được phân công quản lý khách sạn nào.");

            var hotelId = assignment.HotelId;

            // Lấy danh sách tiện nghi hiện tại của khách sạn
            var existing = await _context.HotelAmenities
                .Where(ha => ha.HotelId == hotelId)
                .ToListAsync(cancellationToken);

            var existingIds = existing.Select(e => e.AmenityId).ToHashSet();
            var desiredIds = request.AmenityIds.ToHashSet();

            // Xóa những cái không còn trong danh sách mới
            var toRemove = existing.Where(e => !desiredIds.Contains(e.AmenityId)).ToList();
            _context.HotelAmenities.RemoveRange(toRemove);

            // Thêm những cái mới chưa có
            var toAdd = desiredIds.Where(id => !existingIds.Contains(id))
                .Select(id => new HotelBookingApp.Domain.Models.HotelAmenity
                {
                    HotelId = hotelId,
                    AmenityId = id,
                    AddedAt = DateTime.UtcNow
                });
            await _context.HotelAmenities.AddRangeAsync(toAdd, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return new Response<string>($"Đã cập nhật {desiredIds.Count} tiện nghi cho khách sạn.");
        }
    }
}
