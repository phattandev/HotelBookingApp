using HotelBookingApp.API.Extensions;
using HotelBookingApp.Application.Features.Wards.Commands;
using HotelBookingApp.Application.Features.Wards.Queries;
using HotelBookingApp.Application.Wrapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WardsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public WardsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAllWards([FromQuery] GetWardsQuery query)
        {
            return this.OkOrBadRequest(await _mediator.Send(query));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetWardById(Guid id)
        {
            return this.OkOrBadRequest(await _mediator.Send(new GetWardByIdQuery { Id = id }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateWard(CreateWardCommand command)
        {
            return this.OkOrBadRequest(await _mediator.Send(command));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWard(Guid id, UpdateWardCommand command)
        {
            if (id != command.Id) return BadRequest(new Response<string>("ID không khớp."));
            return this.OkOrBadRequest(await _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWard(Guid id)
        {
            return this.OkOrBadRequest(await _mediator.Send(new DeleteWardCommand { Id = id }));
        }
    }
}