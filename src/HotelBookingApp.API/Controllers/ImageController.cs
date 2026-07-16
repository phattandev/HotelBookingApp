using System.Security.Claims;
using HotelBookingApp.Application.Features.Manager.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    [Authorize(Roles = "manager")]
    [ApiController]
    [Route("api/manager/images")]
    public class ImageController : ControllerBase
    {
        private readonly IMediator _mediator;
        public ImageController(IMediator mediator) => _mediator = mediator;

        private Guid GetManagerId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        /// <summary>Upload ảnh cho khách sạn đang quản lý.</summary>
        [HttpPost("hotel")]
        public async Task<IActionResult> UploadHotelImage(
            IFormFile file,
            [FromQuery] bool setPrimary = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file ảnh.");

            await using var stream = file.OpenReadStream();
            var command = new UploadHotelImageCommand
            {
                ManagerId = GetManagerId(),
                ImageStream = stream,
                FileName = file.FileName,
                SetAsPrimary = setPrimary
            };

            return Ok(await _mediator.Send(command));
        }

        /// <summary>Xóa ảnh khách sạn.</summary>
        [HttpDelete("hotel/{imageId}")]
        public async Task<IActionResult> DeleteHotelImage(Guid imageId)
            => Ok(await _mediator.Send(new DeleteHotelImageCommand
            {
                ManagerId = GetManagerId(),
                ImageId = imageId
            }));

        /// <summary>Upload ảnh cho loại phòng.</summary>
        [HttpPost("roomtype/{roomTypeId}")]
        public async Task<IActionResult> UploadRoomTypeImage(
            Guid roomTypeId,
            IFormFile file,
            [FromQuery] bool setPrimary = false)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng chọn file ảnh.");

            await using var stream = file.OpenReadStream();
            var command = new UploadRoomTypeImageCommand
            {
                ManagerId = GetManagerId(),
                RoomTypeId = roomTypeId,
                ImageStream = stream,
                FileName = file.FileName,
                SetAsPrimary = setPrimary
            };

            return Ok(await _mediator.Send(command));
        }

        /// <summary>Xóa ảnh loại phòng.</summary>
        [HttpDelete("roomtype/{imageId}")]
        public async Task<IActionResult> DeleteRoomTypeImage(Guid imageId)
            => Ok(await _mediator.Send(new DeleteRoomTypeImageCommand
            {
                ManagerId = GetManagerId(),
                ImageId = imageId
            }));
    }
}
