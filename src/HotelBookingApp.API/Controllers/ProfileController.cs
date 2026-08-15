using System.Security.Claims;
using HotelBookingApp.API.Extensions;
using HotelBookingApp.Application.Features.Users.Commands;
using HotelBookingApp.Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ProfileController(IMediator mediator)
        {
            _mediator = mediator;
        }

        // Hàm helper chiết xuất UserId an toàn từ Claims Token
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new UnauthorizedAccessException("Phiên làm việc hết hạn hoặc token không hợp lệ.");
            }
            return Guid.Parse(userIdClaim);
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var response = await _mediator.Send(new GetMyProfileQuery(userId));
            return this.OkOrBadRequest(response);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateMyProfileCommand command)
        {
            command.UserId = GetCurrentUserId(); // Ép UserId lấy từ Token bảo mật, chống giả mạo request
            var response = await _mediator.Send(command);
            return this.OkOrBadRequest(response);
        }
    }
}
