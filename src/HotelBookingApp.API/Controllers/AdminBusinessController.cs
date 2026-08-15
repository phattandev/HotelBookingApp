using HotelBookingApp.API.Extensions;
using HotelBookingApp.Application.Features.Admin.Commands;
using HotelBookingApp.Application.Features.Admin.Queries;
using HotelBookingApp.Application.Features.Hotels.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminBusinessController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminBusinessController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingBusinesses()
        {
            var result = await _mediator.Send(new GetPendingBusinessesQuery());
            return this.OkOrBadRequest(result);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
            => this.OkOrBadRequest(await _mediator.Send(new GetAdminStatsQuery { FromDate = fromDate, ToDate = toDate }));

        [HttpPut("{id}/review")]
        public async Task<IActionResult> ReviewBusiness(Guid id, [FromBody] ReviewBusinessRequest request)
        {
            var result = await _mediator.Send(new ReviewBusinessCommand
            {
                BusinessId = id,
                Action = request.Action,
                RejectionReason = request.RejectionReason
            });
            return this.OkOrBadRequest(result);
        }

        [HttpPut("hotels/{id}/review")]
        public async Task<IActionResult> ReviewHotel(Guid id, [FromBody] ReviewBusinessRequest request)
        {
            var result = await _mediator.Send(new ReviewHotelCommand
            {
                HotelId = id,
                Action = request.Action,
                RejectionReason = request.RejectionReason
            });
            return this.OkOrBadRequest(result);
        }
    }

    public class ReviewBusinessRequest
    {
        public string Action { get; set; } = null!;
        public string? RejectionReason { get; set; }
    }
}
