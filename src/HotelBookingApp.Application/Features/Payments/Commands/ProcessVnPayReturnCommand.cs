using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Application.Wrapper;
using HotelBookingApp.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HotelBookingApp.Application.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HotelBookingApp.Application.Features.Payments.Commands;

public class ProcessVnPayReturnCommand : IRequest<Response<Guid>>
{
    public Dictionary<string, string> QueryData { get; set; } = new();
}

public class ProcessVnPayReturnCommandHandler : IRequestHandler<ProcessVnPayReturnCommand, Response<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public ProcessVnPayReturnCommandHandler(IApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<Response<Guid>> Handle(ProcessVnPayReturnCommand request, CancellationToken cancellationToken)
    {
        var data = request.QueryData;

        var vnp_TxnRef = data.GetValueOrDefault("vnp_TxnRef");
        var vnp_ResponseCode = data.GetValueOrDefault("vnp_ResponseCode");
        var vnp_TransactionNo = data.GetValueOrDefault("vnp_TransactionNo");
        var vnp_SecureHash = data.GetValueOrDefault("vnp_SecureHash") ?? string.Empty;

        var vnp_HashSecret = _configuration["VNPAY:HashSecret"] ?? string.Empty;

        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in data)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value);
            }
        }

        if (!vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret))
        {
            return new Response<Guid>("Chữ ký thanh toán không hợp lệ. Giao dịch có thể đã bị giả mạo.");
        }

        var payment = await _context.Payments
            .Include(p => p.Booking)
            .FirstOrDefaultAsync(p => p.OrderReference == vnp_TxnRef, cancellationToken);

        if (payment == null) return new Response<Guid>("Không tìm thấy thông tin giao dịch.");

        // Nếu giao dịch đã hoàn thành trước đó (do IPN xử lý), ta cứ trả về BookingId để redirect
        if (payment.Status != PaymentTransactionStatus.Pending)
        {
            return new Response<Guid>(payment.BookingId, "Giao dịch đã được xử lý trước đó.");
        }

        payment.TransactionId = vnp_TransactionNo;
        payment.ResponseCode = vnp_ResponseCode;
        payment.CompletedAt = DateTime.UtcNow;

        if (vnp_ResponseCode == "00")
        {
            payment.Status = PaymentTransactionStatus.Success;
            payment.Booking.PaymentStatus = PaymentStatus.Paid;
            payment.Booking.Status = BookingStatus.Confirmed; // Đã cọc thì Confirm đơn luôn
        }
        else
        {
            payment.Status = PaymentTransactionStatus.Failed;
            // Booking status keeps Approved
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new Response<Guid>(payment.BookingId, vnp_ResponseCode == "00" ? "Thanh toán thành công" : "Thanh toán thất bại");
    }
}
