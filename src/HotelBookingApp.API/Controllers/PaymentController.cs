using HotelBookingApp.Application.Common.Helpers;
using HotelBookingApp.Application.Features.Payments.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace HotelBookingApp.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public PaymentController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [Authorize(Roles = "customer")]
    [HttpPost("create-url")]
    public async Task<IActionResult> CreatePaymentUrl([FromBody] CreatePaymentUrlCommand command)
    {
        var remoteIpAddress = HttpContext.Connection.RemoteIpAddress;
        string ipAddress = "127.0.0.1";
        if (remoteIpAddress != null)
        {
            if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                remoteIpAddress = System.Net.Dns.GetHostEntry(remoteIpAddress).AddressList
                    .FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            }
            if (remoteIpAddress != null) ipAddress = remoteIpAddress.ToString();
        }

        // Tự động xây dựng ReturnUrl dựa trên host của request hiện tại
        var dynamicReturnUrl = $"{Request.Scheme}://{Request.Host}/api/payment/vnpay-return";

        command.IpAddress = ipAddress;
        command.ReturnUrl = dynamicReturnUrl;
        var response = await _mediator.Send(command);
        if (response.Succeeded)
            return Ok(response);
        return BadRequest(response);
    }

    [HttpGet("vnpay-return")]
    public async Task<IActionResult> VnPayReturn()
    {
        var queryDictionary = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in queryDictionary)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value);
            }
        }

        var vnp_SecureHash = queryDictionary.GetValueOrDefault("vnp_SecureHash");
        var hashSecret = _configuration["VNPAY:HashSecret"] ?? string.Empty;

        if (string.IsNullOrEmpty(vnp_SecureHash) || !vnpay.ValidateSignature(vnp_SecureHash, hashSecret))
        {
            // Chữ ký không hợp lệ
            return Redirect("http://localhost:5173/my-bookings?payment=invalid_signature");
        }

        var command = new ProcessVnPayReturnCommand { QueryData = queryDictionary };
        var response = await _mediator.Send(command);

        var responseCode = queryDictionary.GetValueOrDefault("vnp_ResponseCode");

        // Chuyển hướng người dùng về trang giao diện frontend
        // BookingId được trả về từ command (Data)
        var bookingId = response.Data;
        var frontendUrl = $"http://localhost:5173/my-bookings?payment={responseCode}&bookingId={bookingId}";
        return Redirect(frontendUrl);
    }
}
