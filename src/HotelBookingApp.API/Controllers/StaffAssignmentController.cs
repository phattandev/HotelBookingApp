using System.Security.Claims;
using HotelBookingApp.API.Extensions;
using HotelBookingApp.Application.Features.StaffAssignment.Commands;
using HotelBookingApp.Application.Features.StaffAssignment.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Authorize(Roles = "partner")]
    [ApiController]
    [Route("api/[controller]")]
    public class StaffAssignmentController : ControllerBase
    {
        private readonly IMediator _mediator;
        public StaffAssignmentController(IMediator mediator) => _mediator = mediator;

        private Guid GetPartnerId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /// <summary>Lấy danh sách tất cả phân công đang active của doanh nghiệp.</summary>
        [HttpGet]
        public async Task<IActionResult> GetAssignments()
        {
            var response = await _mediator.Send(new GetStaffAssignmentsQuery(GetPartnerId()));
            return this.OkOrBadRequest(response);
        }

        /// <summary>Phân công nhân viên vào khách sạn với vai trò cụ thể.</summary>
        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignStaffCommand command)
        {
            command.PartnerId = GetPartnerId();
            return this.OkOrBadRequest(await _mediator.Send(command));
        }

        /// <summary>Hủy phân công của một nhân viên (trả về role staff).</summary>
        [HttpDelete("{employeeId}")]
        public async Task<IActionResult> Unassign(Guid employeeId)
        {
            var command = new UnassignStaffCommand
            {
                PartnerId = GetPartnerId(),
                EmployeeId = employeeId
            };
            return this.OkOrBadRequest(await _mediator.Send(command));
        }
    }
}
