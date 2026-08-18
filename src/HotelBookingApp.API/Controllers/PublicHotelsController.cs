using HotelBookingApp.API.Extensions;
using HotelBookingApp.Application.Features.Hotels.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers
{
    /// <summary>
    /// Controller public cho trang tìm kiếm và chi tiết khách sạn.
    /// Không yêu cầu xác thực JWT — bất kỳ ai cũng có thể gọi.
    /// </summary>
    [Route("api/hotels")]
    [ApiController]
    public class PublicHotelsController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PublicHotelsController(IMediator mediator) => _mediator = mediator;

        /// <summary>Tìm kiếm khách sạn theo từ khóa, khoảng giá, tiện nghi và loại phòng.</summary>
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] SearchHotelsQuery query)
            => this.OkOrBadRequest(await _mediator.Send(query));

        /// <summary>Lấy danh sách tên loại phòng unique (dùng cho bộ lọc tìm kiếm).</summary>
        [HttpGet("room-type-names")]
        public async Task<IActionResult> GetRoomTypeNames()
            => this.OkOrBadRequest(await _mediator.Send(new GetPublicRoomTypeNamesQuery()));

        /// <summary>Gợi ý địa điểm (tỉnh/thành phố và xã/phường) có khách sạn trong hệ thống. Dùng cho autocomplete.</summary>
        [HttpGet("location-suggestions")]
        public async Task<IActionResult> GetLocationSuggestions([FromQuery] string? q, [FromQuery] int limit = 8)
            => this.OkOrBadRequest(await _mediator.Send(new GetLocationSuggestionsQuery { Q = q ?? string.Empty, Limit = limit }));

        /// <summary>Lấy chi tiết 1 khách sạn. Truyền checkIn/checkOut để tính phòng còn trống.</summary>
        [HttpGet("{hotelId}/detail")]
        public async Task<IActionResult> GetDetail(
            Guid hotelId,
            [FromQuery] DateOnly? checkIn,
            [FromQuery] DateOnly? checkOut)
            => this.OkOrBadRequest(await _mediator.Send(new GetHotelPublicDetailQuery
            {
                HotelId = hotelId,
                CheckIn = checkIn,
                CheckOut = checkOut
            }));

        /// <summary>Lấy chi tiết 1 loại phòng. Truyền checkIn/checkOut để tính phòng còn trống.</summary>
        [HttpGet("room-types/{id}")]
        public async Task<IActionResult> GetRoomTypeDetail(
            Guid id,
            [FromQuery] DateOnly? checkIn,
            [FromQuery] DateOnly? checkOut)
            => this.OkOrBadRequest(await _mediator.Send(new GetPublicRoomTypeDetailQuery
            {
                RoomTypeId = id,
                CheckIn = checkIn,
                CheckOut = checkOut
            }));
    }
}
