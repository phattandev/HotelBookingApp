using HotelBookingApp.Application.Features.Amenities.Commands;
using HotelBookingApp.Application.Features.Amenities.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [ApiController]
    [Route("api/amenity-categories")]
    public class AmenityCategoriesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AmenityCategoriesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAll()
            => Ok(await _mediator.Send(new GetAmenityCategoriesQuery()));

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CreateAmenityCategoryCommand command)
            => Ok(await _mediator.Send(command));

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAmenityCategoryCommand command)
        {
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(Guid id)
            => Ok(await _mediator.Send(new DeleteAmenityCategoryCommand { Id = id }));
    }

    [ApiController]
    [Route("api/amenities")]
    public class AmenitiesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public AmenitiesController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] GetAmenitiesQuery query)
            => Ok(await _mediator.Send(query));

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CreateAmenityCommand command)
            => Ok(await _mediator.Send(command));

        [HttpPut("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAmenityCommand command)
        {
            command.Id = id;
            return Ok(await _mediator.Send(command));
        }

        [HttpPatch("{id}/toggle")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Toggle(Guid id)
            => Ok(await _mediator.Send(new ToggleAmenityStatusCommand { Id = id }));
    }
}
