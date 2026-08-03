using System.Security.Claims;
using HotelBookingApp.Application.Features.Hotels.Commands;
using HotelBookingApp.Application.Features.Hotels.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class HotelsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public HotelsController(IMediator mediator) => _mediator = mediator;

        private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // --- PARTNER API ---
        [HttpPost("register")]
        [Authorize(Roles = "partner")]
        public async Task<IActionResult> RegisterHotel([FromBody] RegisterHotelCommand command)
        {
            command.PartnerId = GetUserId();
            return Ok(await _mediator.Send(command));
        }
        [HttpGet("my-hotels")]
        [Authorize(Roles = "partner")]
        public async Task<IActionResult> GetMyHotels()
        {
            var query = new GetMyHotelsQuery { PartnerId = GetUserId() };
            return Ok(await _mediator.Send(query));
        }

        [HttpGet("my-hotels/{id}")]
        [Authorize(Roles = "partner")]
        public async Task<IActionResult> GetMyHotelDetail(Guid id)
        {
            var query = new GetPartnerHotelDetailQuery(GetUserId(), id);
            return Ok(await _mediator.Send(query));
        }

        [HttpPost("{id}/submit")]
        [Authorize(Roles = "partner")]
        public async Task<IActionResult> SubmitHotelRegistration(Guid id)
        {
            var command = new SubmitHotelRegistrationCommand
            {
                HotelId = id,
                PartnerId = GetUserId()
            };
            return Ok(await _mediator.Send(command));
        }

        [HttpPut("{id}/basic-info")]
        [Authorize(Roles = "partner,manager")]
        public async Task<IActionResult> UpdateBasicInfo(Guid id, [FromBody] UpdateHotelBasicInfoCommand command)
        {
            command.HotelId = id;
            command.UserId = GetUserId();
            command.Role = User.IsInRole("partner") ? "partner" : "manager";
            return Ok(await _mediator.Send(command));
        }

        // --- ADMIN API ---
        [HttpPut("{id}/review")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> ReviewHotel(Guid id, [FromBody] ReviewHotelCommand command)
        {
            command.HotelId = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpGet("pending")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetPendingHotels()
        {
            return Ok(await _mediator.Send(new GetPendingHotelsQuery()));
        }
    }
}
