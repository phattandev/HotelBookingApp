using System.Security.Claims;
using HotelBookingApp.Application.Features.Bookings.Commands;
using HotelBookingApp.Application.Features.Bookings.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    /// <summary>
    /// Controller xử lý đặt phòng và quản lý đơn của khách hàng.
    /// Tất cả endpoint đều yêu cầu đăng nhập.
    /// </summary>
    [Route("api/bookings")]
    [ApiController]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IMediator _mediator;
        public BookingController(IMediator mediator) => _mediator = mediator;

        /// <summary>Lấy CustomerId từ JWT claim.</summary>
        private Guid GetCurrentUserId()
            => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /// <summary>Tạo đơn đặt phòng mới.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingCommand command)
        {
            command.CustomerId = GetCurrentUserId();
            return Ok(await _mediator.Send(command));
        }

        /// <summary>Lấy danh sách đơn của khách hàng đang đăng nhập.</summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings([FromQuery] string? status)
            => Ok(await _mediator.Send(new GetMyBookingsQuery
            {
                CustomerId = GetCurrentUserId(),
                StatusFilter = status
            }));

        /// <summary>Xem chi tiết 1 đơn đặt phòng (chỉ chủ đơn mới xem được).</summary>
        [HttpGet("{bookingId}")]
        public async Task<IActionResult> GetDetail(Guid bookingId)
            => Ok(await _mediator.Send(new GetBookingDetailQuery
            {
                BookingId = bookingId,
                CustomerId = GetCurrentUserId()
            }));

        /// <summary>Khách hàng tự hủy đơn (chỉ khi Pending, cần nhập lý do).</summary>
        [HttpPut("{bookingId}/cancel")]
        public async Task<IActionResult> Cancel(Guid bookingId, [FromBody] CancelBookingRequest request)
            => Ok(await _mediator.Send(new CancelBookingCommand
            {
                BookingId = bookingId,
                CustomerId = GetCurrentUserId(),
                CancelReason = request.CancelReason
            }));

        /// <summary>Giả lập thanh toán cọc (Dev/Test only) — tự động Confirm đơn nếu Pending.</summary>
        [HttpPost("{bookingId}/mock-payment")]
        public async Task<IActionResult> MockPayment(Guid bookingId)
            => Ok(await _mediator.Send(new MockPaymentCommand
            {
                BookingId = bookingId,
                CustomerId = GetCurrentUserId()
            }));
    }

    public class CancelBookingRequest
    {
        public string CancelReason { get; set; } = null!;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Controller dành cho Manager quản lý đơn đặt phòng của khách sạn
    // ─────────────────────────────────────────────────────────────────────────────

    [Route("api/manager/bookings")]
    [ApiController]
    [Authorize(Roles = "manager")]
    public class ManagerBookingController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ManagerBookingController(IMediator mediator) => _mediator = mediator;

        private Guid GetManagerId()
            => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /// <summary>Lấy danh sách đơn đặt phòng của khách sạn đang quản lý.</summary>
        [HttpGet]
        public async Task<IActionResult> GetHotelBookings([FromQuery] string? status)
            => Ok(await _mediator.Send(new GetHotelBookingsQuery
            {
                ManagerId = GetManagerId(),
                StatusFilter = status
            }));

        /// <summary>Xác nhận hoặc từ chối đơn đặt phòng. Action: "confirm" hoặc "reject".</summary>
        [HttpPut("{bookingId}/status")]
        public async Task<IActionResult> UpdateStatus(Guid bookingId, [FromBody] UpdateStatusRequest request)
            => Ok(await _mediator.Send(new UpdateBookingStatusCommand
            {
                BookingId = bookingId,
                ManagerId = GetManagerId(),
                Action = request.Action,
                CancelReason = request.CancelReason
            }));
    }

    public class UpdateStatusRequest
    {
        public string Action { get; set; } = null!;
        public string? CancelReason { get; set; }
    }
}
