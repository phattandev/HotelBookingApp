using System.Security.Claims;
using HotelBookingApp.Application.Features.Reviews.Commands;
using HotelBookingApp.Application.Features.Reviews.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelBookingApp.API.Controllers;

[ApiController]
[Route("api/reviews")]
public class ReviewController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReviewController(IMediator mediator) => _mediator = mediator;

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    /// <summary>
    /// Lấy danh sách đánh giá của khách sạn (public, không cần đăng nhập).
    /// </summary>
    [HttpGet("hotel/{hotelId}")]
    public async Task<IActionResult> GetHotelReviews(Guid hotelId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        => Ok(await _mediator.Send(new GetHotelReviewsQuery { HotelId = hotelId, Page = page, PageSize = pageSize }));

    /// <summary>
    /// Khách hàng gửi đánh giá (yêu cầu đăng nhập, đơn phải Completed).
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReview([FromBody] CreateReviewCommand command)
    {
        command.CustomerId = GetCurrentUserId();
        return Ok(await _mediator.Send(command));
    }
}
