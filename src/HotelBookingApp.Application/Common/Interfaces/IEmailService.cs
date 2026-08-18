namespace HotelBookingApp.Application.Common.Interfaces;

using HotelBookingApp.Domain.Models;

/// <summary>
/// Interface gửi email thông báo cho khách hàng.
/// </summary>
public interface IEmailService
{
    /// <summary>Gửi email đặt cọc nhắc nhở thanh toán.</summary>
    Task SendDepositReminderAsync(Booking booking);

    /// <summary>Gửi email thông báo quản lý đã duyệt và yêu cầu cọc.</summary>
    Task SendBookingApprovedAsync(Booking booking);

    /// <summary>Gửi email xác nhận đặt phòng thành công (sau khi đã cọc hoặc không cần cọc).</summary>
    Task SendBookingConfirmedAsync(Booking booking);

    /// <summary>Gửi email thông báo hủy đơn.</summary>
    Task SendBookingCancelledAsync(Booking booking);

    /// <summary>Gửi email xác nhận đã nhận cọc.</summary>
    Task SendDepositConfirmedAsync(Booking booking);

    /// <summary>Gửi email thông báo hoàn cọc.</summary>
    Task SendDepositRefundedAsync(Booking booking);

    /// <summary>Gửi email cảm ơn sau khi khách đã chót checkout (chuyển Completed).</summary>
    Task SendPostCheckoutThankYouAsync(Booking booking);
}
