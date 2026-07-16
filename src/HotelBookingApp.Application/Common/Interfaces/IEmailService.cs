namespace HotelBookingApp.Application.Common.Interfaces;

/// <summary>
/// Interface gửi email thông báo cho khách hàng.
/// </summary>
public interface IEmailService
{
    /// <summary>Gửi email đặt cọc nhắc nhở thanh toán.</summary>
    Task SendDepositReminderAsync(string toEmail, string guestName, string bookingId, decimal depositAmount, DateTime deadline);

    /// <summary>Gửi email thông báo quản lý đã duyệt và yêu cầu cọc.</summary>
    Task SendBookingApprovedAsync(string toEmail, string guestName, string bookingId, decimal depositAmount, DateTime deadline);

    /// <summary>Gửi email xác nhận đặt phòng thành công (sau khi đã cọc hoặc không cần cọc).</summary>
    Task SendBookingConfirmedAsync(string toEmail, string guestName, string bookingId);

    /// <summary>Gửi email thông báo hủy đơn.</summary>
    Task SendBookingCancelledAsync(string toEmail, string guestName, string bookingId, string reason);

    /// <summary>Gửi email xác nhận đã nhận cọc.</summary>
    Task SendDepositConfirmedAsync(string toEmail, string guestName, string bookingId, decimal depositAmount);

    /// <summary>Gửi email thông báo hoàn cọc.</summary>
    Task SendDepositRefundedAsync(string toEmail, string guestName, string bookingId, decimal refundAmount);

    /// <summary>Gửi email cảm ơn sau khi khách đã chót checkout (chuyển Completed).</summary>
    Task SendPostCheckoutThankYouAsync(string toEmail, string guestName, string bookingId, string hotelName, DateOnly checkOutDate);
}
