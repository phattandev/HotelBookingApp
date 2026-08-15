using HotelBookingApp.API.Extensions;
using HotelBookingApp.Application.DTOs.AuthDto;
using HotelBookingApp.Application.Features.Auth.Commands.Login;
using HotelBookingApp.Application.Features.Auth.Commands.RefreshToken;
using HotelBookingApp.Application.Features.Auth.Commands.RegisterBusiness;
using HotelBookingApp.Application.Features.Auth.Commands.RegisterUser;
using HotelBookingApp.Application.Wrapper;
using MediatR;
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
            return this.OkOrBadRequest(response);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenCommand command)
        {
            Response<AuthResponseDto>? response = await _mediator.Send(command);
            return this.OkOrBadRequest(response);
        }

        [HttpPost("register/user")]
        public async Task<IActionResult> RegisterUser([FromBody] RegisterUserCommand command)
        {
            Response<AuthResponseDto> response = await _mediator.Send(command);
            return this.OkOrBadRequest(response);
        }

        [HttpPost("register/business")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RegisterBusiness([FromForm] RegisterBusinessCommand command)
        {
            Response<string> response = await _mediator.Send(command);
            return this.OkOrBadRequest(response);
        }
    }
}
