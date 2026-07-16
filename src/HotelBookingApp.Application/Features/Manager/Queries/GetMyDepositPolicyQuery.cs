using HotelBookingApp.Application.Common.Exceptions;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Features.Manager.Commands;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Features.Manager.Queries
{
    /// <summary>
    /// Lấy chính sách đặt cọc của khách sạn mà Manager đang quản lý.
    /// </summary>
    public class GetMyDepositPolicyQuery : IRequest<Response<DepositPolicyDto?>>
    {
        public Guid ManagerId { get; set; }
    }

    public class GetMyDepositPolicyQueryHandler
        : IRequestHandler<GetMyDepositPolicyQuery, Response<DepositPolicyDto?>>
    {
        private readonly IApplicationDbContext _context;
        public GetMyDepositPolicyQueryHandler(IApplicationDbContext context) => _context = context;

        public async Task<Response<DepositPolicyDto?>> Handle(
            GetMyDepositPolicyQuery request, CancellationToken cancellationToken)
        {
            var assignment = await _context.HotelStaffAssignments
                .FirstOrDefaultAsync(a =>
                    a.UserId == request.ManagerId &&
                    a.RoleInHotel == "manager" &&
                    a.IsActive,
                    cancellationToken);

            if (assignment == null)
                throw new ApiException("Bạn không được phân công quản lý khách sạn nào.");

            var policy = await _context.HotelDepositPolicies
                .FirstOrDefaultAsync(p => p.HotelId == assignment.HotelId, cancellationToken);

            if (policy == null)
                return new Response<DepositPolicyDto?>(null, "Khách sạn chưa thiết lập chính sách đặt cọc.");

            return new Response<DepositPolicyDto?>(DepositPolicyMapper.ToDto(policy),
                "Lấy chính sách đặt cọc thành công.");
        }
    }
}
