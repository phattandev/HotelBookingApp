using System.Security.Claims;
using HotelBookingApp.Application.Features.BusinessStaff.Commands;
using HotelBookingApp.Application.Features.BusinessStaff.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Authorize(Roles = "partner")] // Cực kỳ quan trọng: Chỉ Partner mới gọi được API này
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessStaffController : ControllerBase
    {
        private readonly IMediator _mediator;

        public BusinessStaffController(IMediator mediator) => _mediator = mediator;

        private Guid GetPartnerId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        [HttpGet]
        public async Task<IActionResult> GetEmployees()
        {
            var response = await _mediator.Send(new GetBusinessEmployeesQuery(GetPartnerId()));
            return Ok(response);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var query = new GetBusinessStatsQuery { UserId = GetPartnerId(), FromDate = fromDate, ToDate = toDate };
            return Ok(await _mediator.Send(query));
        }

        [HttpPost]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeCommand command)
        {
            command.PartnerId = GetPartnerId();
            var response = await _mediator.Send(command);
            return Ok(response);
        }

        [HttpPut("{employeeId}")]
        public async Task<IActionResult> UpdateEmployee(Guid employeeId, [FromBody] UpdateEmployeeCommand command)
        {
            command.PartnerId = GetPartnerId();
            command.EmployeeId = employeeId;
            return Ok(await _mediator.Send(command));
        }

        [HttpPatch("{employeeId}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid employeeId)
        {
            var response = await _mediator.Send(new ToggleEmployeeStatusCommand { PartnerId = GetPartnerId(), EmployeeId = employeeId });
            return Ok(response);
        }
    }
}
