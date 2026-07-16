using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Features.Manager.Commands;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Queries
{
    /// <summary>
    /// Lấy chính sách hủy phòng của 1 khách sạn cụ thể.
    /// Dùng cho: trang chi tiết khách sạn (public) + Manager dashboard.
    /// </summary>
    public class GetHotelCancellationPolicyQuery : IRequest<Response<CancellationPolicyDto?>>
    {
        public Guid HotelId { get; set; }
    }

    public class GetHotelCancellationPolicyQueryHandler
        : IRequestHandler<GetHotelCancellationPolicyQuery, Response<CancellationPolicyDto?>>
    {
        private readonly IApplicationDbContext _context;
        public GetHotelCancellationPolicyQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<CancellationPolicyDto?>> Handle(
            GetHotelCancellationPolicyQuery request, CancellationToken cancellationToken)
        {
            var policy = await _context.HotelCancellationPolicies
                .FirstOrDefaultAsync(p => p.HotelId == request.HotelId && p.IsActive, cancellationToken);

            if (policy == null)
                return new Response<CancellationPolicyDto?>(null, "Khách sạn chưa thiết lập chính sách hủy phòng.");

            return new Response<CancellationPolicyDto?>(CancellationPolicyMapper.ToDto(policy),
                "Lấy chính sách hủy phòng thành công.");
        }
    }

    /// <summary>
    /// Lấy chính sách của khách sạn mà Manager đang quản lý (từ JWT).
    /// </summary>
    public class GetMyHotelCancellationPolicyQuery : IRequest<Response<CancellationPolicyDto?>>
    {
        public Guid ManagerId { get; set; }
    }

    public class GetMyHotelCancellationPolicyQueryHandler
        : IRequestHandler<GetMyHotelCancellationPolicyQuery, Response<CancellationPolicyDto?>>
    {
        private readonly IApplicationDbContext _context;
        public GetMyHotelCancellationPolicyQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<CancellationPolicyDto?>> Handle(
            GetMyHotelCancellationPolicyQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.UserId == request.ManagerId &&
                    a.RoleInHotel == "manager" &&
                    a.IsActive,
                    cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không được phân công quản lý khách sạn nào.");

            var policy = await _context.HotelCancellationPolicies
                .FirstOrDefaultAsync(p => p.HotelId == assignment.HotelId, cancellationToken);

            if (policy == null)
                return new Response<CancellationPolicyDto?>(null, "Khách sạn chưa thiết lập chính sách hủy phòng.");

            return new Response<CancellationPolicyDto?>(CancellationPolicyMapper.ToDto(policy),
                "Lấy chính sách hủy phòng thành công.");
        }
    }
}
