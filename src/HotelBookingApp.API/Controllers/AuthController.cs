using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Features.Auth.Commands.Login;
using HotelBookingApp.Application.Features.Auth.Commands.RefreshToken;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IMediator _mediator;

        public AuthController (IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginCommand command)
        {
            Response<AuthResponseDto> response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            Response<AuthResponseDto>? response = await _mediator.Send(command);
            return Ok(response);
        }
    }
}
