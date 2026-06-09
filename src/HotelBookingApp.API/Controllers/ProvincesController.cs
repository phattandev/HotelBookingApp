using HotelBookingApp.Application.Features.Provinces.Commands;
using HotelBookingApp.Application.Features.Provinces.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProvincesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProvincesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAllProvinces()
        {
            return Ok(await _mediator.Send(new GetProvincesQuery()));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProvinceById(Guid id)
        {
            return Ok(await _mediator.Send(new GetProvinceByIdQuery { Id = id }));
        }

        [HttpPost]
        public async Task<IActionResult> CreateProvince(CreateProvinceCommand command)
        {
            return Ok(await _mediator.Send(command));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProvince(Guid id, UpdateProvinceCommand command)
        {
            if (id != command.Id) return BadRequest("ID in URL does not match ID in body.");
            return Ok(await _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProvince(Guid id)
        {
            return Ok(await _mediator.Send(new DeleteProvinceCommand { Id = id }));
        }
    }
}