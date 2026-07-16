using System.Security.Claims;
using HotelBookingApp.Application.Features.Admin.Commands;
using HotelBookingApp.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // --- Quản lý khách sạn ---
        [HttpGet("hotels")]
        public async Task<IActionResult> GetAllHotels([FromQuery] AdminGetAllHotelsQuery query)
        {
            return Ok(await _mediator.Send(query));
        }

        [HttpPatch("hotels/{id}/toggle-active")]
        public async Task<IActionResult> ToggleHotelActive(Guid id)
        {
            var command = new ToggleHotelActiveCommand { HotelId = id };
            return Ok(await _mediator.Send(command));
        }

        // --- Quản lý tài khoản ---
        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllUsers([FromQuery] AdminGetAllUsersQuery query)
        {
            return Ok(await _mediator.Send(query));
        }

        [HttpPatch("accounts/{id}/toggle-status")]
        public async Task<IActionResult> ToggleUserStatus(Guid id)
        {
            var command = new ToggleUserStatusCommand 
            { 
                TargetUserId = id,
                AdminId = GetUserId()
            };
            return Ok(await _mediator.Send(command));
        }
    }
}
