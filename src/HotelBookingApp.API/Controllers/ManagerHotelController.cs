using System.Security.Claims;
using HotelBookingApp.Application.Features.Manager.Commands;
using HotelBookingApp.Application.Features.Manager.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Authorize(Roles = "manager")]
    [ApiController]
    [Route("api/manager/hotel")]
    public class ManagerHotelController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ManagerHotelController(IMediator mediator) => _mediator = mediator;

        private Guid GetManagerId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /// <summary>Lấy thông tin đầy đủ khách sạn đang quản lý (kèm ảnh, tiện nghi, loại phòng).</summary>
        [HttpGet]
        public async Task<IActionResult> GetMyHotel()
            => Ok(await _mediator.Send(new GetMyManagedHotelQuery(GetManagerId())));

        /// <summary>Cập nhật mô tả và số sao khách sạn.</summary>
        [HttpPut("info")]
        public async Task<IActionResult> UpdateInfo([FromBody] UpdateHotelInfoCommand command)
        {
            command.ManagerId = GetManagerId();
            return Ok(await _mediator.Send(command));
        }

        /// <summary>Đồng bộ tiện nghi khách sạn (gửi danh sách IDs muốn giữ lại).</summary>
        [HttpPut("amenities")]
        public async Task<IActionResult> SyncAmenities([FromBody] SyncHotelAmenitiesCommand command)
        {
            command.ManagerId = GetManagerId();
            return Ok(await _mediator.Send(command));
        }

        /// <summary>Lấy tất cả tiện nghi admin cung cấp (lọc theo type: hotel/room).</summary>
        [HttpGet("amenities/catalog")]
        public async Task<IActionResult> GetAmenityCatalog([FromQuery] string? type = null)
            => Ok(await _mediator.Send(new GetAmenitiesForManagerQuery(type)));
    }
}
