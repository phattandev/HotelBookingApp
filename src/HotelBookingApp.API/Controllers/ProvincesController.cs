using HotelBookingApp.API.Extensions;
using HotelBookingApp.Application.Features.Provinces.Commands;
using HotelBookingApp.Application.Features.Provinces.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProvincesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ProvincesController(IMediator mediator) => _mediator = mediator;

        [HttpGet] // Mọi người đều có thể xem danh sách tỉnh thành để chọn
        public async Task<IActionResult> GetAll([FromQuery] bool includeHidden = false) => this.OkOrBadRequest(await _mediator.Send(new GetProvincesQuery { IncludeHidden = includeHidden }));

        [HttpPost]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Create([FromBody] CreateProvinceCommand command) => this.OkOrBadRequest(await _mediator.Send(command));

        [HttpPut]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Update([FromBody] UpdateProvinceCommand command) => this.OkOrBadRequest(await _mediator.Send(command));

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> Delete(Guid id) => this.OkOrBadRequest(await _mediator.Send(new DeleteProvinceCommand { Id = id }));
    }
}