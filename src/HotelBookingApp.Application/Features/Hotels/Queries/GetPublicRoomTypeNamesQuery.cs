using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Hotels.Queries
{
    /// <summary>
    /// Query lấy danh sách tên loại phòng unique từ toàn bộ khách sạn đã duyệt.
    /// Dùng cho bộ lọc tìm kiếm ở trang public.
    /// </summary>
    public class GetPublicRoomTypeNamesQuery : IRequest<Response<List<string>>>
    {
    }

    public class GetPublicRoomTypeNamesQueryHandler : IRequestHandler<GetPublicRoomTypeNamesQuery, Response<List<string>>>
    {
        private readonly IApplicationDbContext _context;
        public GetPublicRoomTypeNamesQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<List<string>>> Handle(GetPublicRoomTypeNamesQuery request, CancellationToken cancellationToken)
        {
            var names = await _context.Hotels
                .Where(h => h.ApprovalStatus == Domain.Models.HotelApprovalStatus.Approved && h.IsActive)
                .SelectMany(h => h.RoomTypes)
                .Where(rt => rt.IsActive)
                .Select(rt => rt.Name)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync(cancellationToken);

            return new Response<List<string>>(names, $"Tìm thấy {names.Count} loại phòng.");
        }
    }
}
