using System.Security.Claims;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Features.Admin.Commands;
using HotelBookingApp.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IApplicationDbContext _context;

        public AdminController(IMediator mediator, IApplicationDbContext context)
        {
            _mediator = mediator;
            _context = context;
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

        // --- Quản lý đặt phòng toàn nền tảng ---

        [HttpGet("booking-stats")]
        public async Task<IActionResult> GetBookingStats([FromQuery] GetAdminBookingStatsQuery query)
        {
            return Ok(await _mediator.Send(query));
        }

        /// <summary>
        /// Trả về danh sách doanh nghiệp và khách sạn cho dropdown bộ lọc.
        /// </summary>
        [HttpGet("filter-options")]
        public async Task<IActionResult> GetFilterOptions()
        {
            var businesses = await _context.Businesses
                .OrderBy(b => b.BusinessName)
                .Select(b => new { id = b.Id, name = b.BusinessName })
                .ToListAsync();

            var hotels = await _context.Hotels
                .OrderBy(h => h.Name)
                .Select(h => new { id = h.Id, name = h.Name, businessId = h.BusinessId })
                .ToListAsync();

            return Ok(new { businesses, hotels });
        }
    }
}
