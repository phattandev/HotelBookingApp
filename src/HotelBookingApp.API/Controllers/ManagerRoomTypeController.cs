using System.Security.Claims;
using HotelBookingApp.Application.Features.Manager.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Authorize(Roles = "manager")]
    [ApiController]
    [Route("api/manager/roomtypes")]
    public class ManagerRoomTypeController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ManagerRoomTypeController(IMediator mediator) => _mediator = mediator;

        private Guid GetManagerId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /// <summary>Tạo loại phòng mới cho khách sạn đang quản lý.</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRoomTypeCommand command)
        {
            command.ManagerId = GetManagerId();
            return Ok(await _mediator.Send(command));
        }

        /// <summary>Cập nhật thông tin và tiện nghi loại phòng.</summary>
        [HttpPut("{roomTypeId}")]
        public async Task<IActionResult> Update(Guid roomTypeId, [FromBody] UpdateRoomTypeCommand command)
        {
            command.ManagerId = GetManagerId();
            command.RoomTypeId = roomTypeId;
            return Ok(await _mediator.Send(command));
        }

        /// <summary>Xóa mềm loại phòng (IsActive = false).</summary>
        [HttpDelete("{roomTypeId}")]
        public async Task<IActionResult> Delete(Guid roomTypeId)
            => Ok(await _mediator.Send(new DeleteRoomTypeCommand
            {
                ManagerId = GetManagerId(),
                RoomTypeId = roomTypeId
            }));

        /// <summary>Khôi phục loại phòng đã ẩn (IsActive = true).</summary>
        [HttpPut("{roomTypeId}/restore")]
        public async Task<IActionResult> Restore(Guid roomTypeId)
            => Ok(await _mediator.Send(new RestoreRoomTypeCommand
            {
                ManagerId = GetManagerId(),
                RoomTypeId = roomTypeId
            }));
    }
}
