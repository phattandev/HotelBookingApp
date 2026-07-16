using System.Security.Claims;
using HotelBookingApp.Application.Features.Manager.Commands;
using HotelBookingApp.Application.Features.Manager.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    /// <summary>
    /// Controller quản lý chính sách hủy phòng.
    /// Manager tạo/sửa policy cho khách sạn mình quản lý.
    /// Public có thể GET policy của bất kỳ hotel nào.
    /// </summary>
    [ApiController]
    public class CancellationPolicyController : ControllerBase
    {
        private readonly IMediator _mediator;
        public CancellationPolicyController(IMediator mediator) => _mediator = mediator;

        private Guid GetCurrentUserId()
            => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // ── Public ────────────────────────────────────────────────────────

        /// <summary>Xem chính sách hủy phòng của khách sạn (không cần đăng nhập).</summary>
        [HttpGet("api/hotels/{hotelId}/policy")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPolicy(Guid hotelId)
            => Ok(await _mediator.Send(new GetHotelCancellationPolicyQuery { HotelId = hotelId }));

        // ── Manager: Cancellation Policy ──────────────────────────────────

        /// <summary>Manager xem chính sách hủy của khách sạn mình đang quản lý.</summary>
        [HttpGet("api/manager/policy")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> GetMyPolicy()
            => Ok(await _mediator.Send(new GetMyHotelCancellationPolicyQuery
            {
                ManagerId = GetCurrentUserId()
            }));

        /// <summary>Manager tạo chính sách hủy phòng (chỉ khi chưa có).</summary>
        [HttpPost("api/manager/policy")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Create([FromBody] PolicyRequest request)
        {
            var result = await _mediator.Send(new CreateCancellationPolicyCommand
            {
                ManagerId = GetCurrentUserId(),
                PolicyName = request.PolicyName,
                HoursBeforeCheckIn = request.HoursBeforeCheckIn,
                PenaltyPercentage = request.PenaltyPercentage
            });
            return Ok(result);
        }

        /// <summary>Manager cập nhật chính sách hủy phòng hiện có.</summary>
        [HttpPut("api/manager/policy")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> Update([FromBody] UpdatePolicyRequest request)
        {
            var result = await _mediator.Send(new UpdateCancellationPolicyCommand
            {
                ManagerId = GetCurrentUserId(),
                PolicyName = request.PolicyName,
                HoursBeforeCheckIn = request.HoursBeforeCheckIn,
                PenaltyPercentage = request.PenaltyPercentage,
                IsActive = request.IsActive
            });
            return Ok(result);
        }

        // ── Manager: Deposit Policy ────────────────────────────────────────

        /// <summary>Manager xem chính sách đặt cọc của khách sạn mình đang quản lý.</summary>
        [HttpGet("api/manager/deposit-policy")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> GetMyDepositPolicy()
            => Ok(await _mediator.Send(new GetMyDepositPolicyQuery
            {
                ManagerId = GetCurrentUserId()
            }));

        /// <summary>Manager tạo chính sách đặt cọc (chỉ khi chưa có).</summary>
        [HttpPost("api/manager/deposit-policy")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> CreateDepositPolicy([FromBody] DepositPolicyRequest request)
        {
            var result = await _mediator.Send(new CreateDepositPolicyCommand
            {
                ManagerId = GetCurrentUserId(),
                HoursBeforeCheckIn = request.HoursBeforeCheckIn,
                DepositPercentage = request.DepositPercentage
            });
            return Ok(result);
        }

        /// <summary>Manager cập nhật chính sách đặt cọc hiện có.</summary>
        [HttpPut("api/manager/deposit-policy")]
        [Authorize(Roles = "manager")]
        public async Task<IActionResult> UpdateDepositPolicy([FromBody] UpdateDepositPolicyRequest request)
        {
            var result = await _mediator.Send(new UpdateDepositPolicyCommand
            {
                ManagerId = GetCurrentUserId(),
                HoursBeforeCheckIn = request.HoursBeforeCheckIn,
                DepositPercentage = request.DepositPercentage,
                IsActive = request.IsActive
            });
            return Ok(result);
        }
    }

    // ── Request DTOs ────────────────────────────────────────────────────────

    public class PolicyRequest
    {
        public string PolicyName { get; set; } = null!;
        public int HoursBeforeCheckIn { get; set; }
        public decimal PenaltyPercentage { get; set; }
    }

    public class UpdatePolicyRequest : PolicyRequest
    {
        public bool IsActive { get; set; } = true;
    }

    public class DepositPolicyRequest
    {
        public int HoursBeforeCheckIn { get; set; }
        public decimal DepositPercentage { get; set; }
    }

    public class UpdateDepositPolicyRequest : DepositPolicyRequest
    {
        public bool IsActive { get; set; } = true;
    }
}
