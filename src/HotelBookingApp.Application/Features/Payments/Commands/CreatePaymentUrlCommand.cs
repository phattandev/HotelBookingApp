using HotelBookingApp.Application.Common.Helpers;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HotelBookingApp.Application.Features.Payments.Commands;

public class CreatePaymentUrlCommand : IRequest<Response<string>>
{
    public Guid BookingId { get; set; }
    /// <summary>Set bởi controller từ HTTP context, không cần gửi từ frontend.</summary>
    public string? IpAddress { get; set; }
    /// <summary>ReturnUrl động — controller tự xây dựng từ request host. Được ưu tiên hơn config.</summary>
    public string? ReturnUrl { get; set; }
}

public class CreatePaymentUrlCommandHandler : IRequestHandler<CreatePaymentUrlCommand, Response<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public CreatePaymentUrlCommandHandler(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<Response<string>> Handle(CreatePaymentUrlCommand request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);
        if (booking == null) return new Response<string>("Không tìm thấy đơn đặt phòng.");

        if (booking.Status != BookingStatus.Approved && booking.Status != BookingStatus.Confirmed)
            return new Response<string>("Chỉ có thể thanh toán đơn đã được duyệt hoặc xác nhận.");

        if (booking.PaymentStatus == PaymentStatus.Paid)
            return new Response<string>("Đơn này đã được thanh toán cọc.");

        if (booking.DepositAmount <= 0)
            return new Response<string>("Đơn này không yêu cầu cọc.");

        var vnp_Returnurl = _configuration["VNPAY:ReturnUrl"];
        var vnp_Url = _configuration["VNPAY:BaseUrl"];
        var vnp_TmnCode = _configuration["VNPAY:TmnCode"];
        var vnp_HashSecret = _configuration["VNPAY:HashSecret"];
        var vnp_Version = _configuration["VNPAY:Version"];

        // Tạo order reference mới cho lần thanh toán này
        var orderId = DateTime.Now.Ticks.ToString();

        // Tạo bản ghi Payment
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Amount = booking.DepositAmount,
            OrderReference = orderId,
            PaymentMethod = "VNPAY",
            Status = PaymentTransactionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        // Build VNPAY URL
        var vnpay = new VnPayLibrary();
        vnpay.AddRequestData("vnp_Version", vnp_Version ?? "2.1.0");
        vnpay.AddRequestData("vnp_Command", "pay");
        vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode ?? "");
        // VNPAY amount is multiplied by 100
        vnpay.AddRequestData("vnp_Amount", ((long)(payment.Amount * 100)).ToString());
        vnpay.AddRequestData("vnp_CreateDate", payment.CreatedAt.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", "VND");
        vnpay.AddRequestData("vnp_IpAddr", request.IpAddress ?? "127.0.0.1");
        vnpay.AddRequestData("vnp_Locale", "vn");
        vnpay.AddRequestData("vnp_OrderInfo", $"Thanh toan coc cho don dat phong: {booking.Id}");
        vnpay.AddRequestData("vnp_OrderType", "other");
        vnpay.AddRequestData("vnp_ReturnUrl", request.ReturnUrl ?? vnp_Returnurl ?? "");
        vnpay.AddRequestData("vnp_TxnRef", orderId);

        var paymentUrl = vnpay.CreateRequestUrl(vnp_Url ?? "", vnp_HashSecret ?? "");

        return new Response<string>(paymentUrl, "Thành công");
    }
}
