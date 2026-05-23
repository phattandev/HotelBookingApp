using HotelBookingApp.Application.Features.Users.Commands;
using HotelBookingApp.Application.Features.Users.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;
        public UsersController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAllUsers()
        {
            return Ok(await _mediator.Send(new GetUsersQuery()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            return Ok(await _mediator.Send(new GetUserByIdQuery { Id = id }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CreateUserCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, UpdateUserCommand command)
        {
            if (id != command.Id) return BadRequest("ID mismatches.");
            return Ok(await _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            return Ok(await _mediator.Send(new DeleteUserCommand { Id = id }));
        }
    }
}