using System.Net;
using System.Net.Mail;
using HotelBookingApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HotelBookingApp.Infrastructure.Services;

/// <summary>
/// Triển khai IEmailService dùng SMTP Gmail (App Password).
/// Cấu hình trong appsettings.Development.json mục SmtpSettings.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    private string Host => _config["SmtpSettings:Host"] ?? "smtp.gmail.com";
    private int Port => int.Parse(_config["SmtpSettings:Port"] ?? "587");
    private string FromEmail => _config["SmtpSettings:Email"] ?? "";
    private string AppPassword => _config["SmtpSettings:AppPassword"] ?? "";
    private string DisplayName => _config["SmtpSettings:DisplayName"] ?? "HotelBooking";

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, string? customDisplayName = null)
    {
        try
        {
            using var client = new SmtpClient(Host, Port)
            {
                Credentials = new NetworkCredential(FromEmail, AppPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var finalDisplayName = string.IsNullOrWhiteSpace(customDisplayName) ? DisplayName : customDisplayName;

            var message = new MailMessage
            {
                From = new MailAddress(FromEmail, finalDisplayName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
            _logger.LogInformation("[EmailService] Đã gửi email '{Subject}' đến {Email}", subject, toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailService] Lỗi khi gửi email đến {Email}: {Message}", toEmail, ex.Message);
            // Không throw để tránh ảnh hưởng business flow
        }
    }

    public Task SendBookingApprovedAsync(string toEmail, string guestName, string bookingId, decimal depositAmount, DateTime deadline)
    {
        string subject = $"[BookNow] Đơn đặt phòng #{bookingId[..8]} đã được duyệt - Yêu cầu đặt cọc";
        string vnTime = deadline.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
        string html = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; padding: 20px; border: 1px solid #ddd; border-radius: 10px;'>
                <h2 style='color: #4CAF50; text-align: center;'>Đơn Đặt Phòng Đã Được Duyệt!</h2>
                <p>Xin chào <b>{guestName}</b>,</p>
                <p>Khách sạn đã duyệt yêu cầu đặt phòng của bạn (Mã: <b>{bookingId}</b>).</p>
                <p>Để hoàn tất quá trình giữ chỗ, vui lòng thanh toán khoản tiền cọc <b>{depositAmount:N0} VNĐ</b>.</p>
                <p style='color: #d9534f; font-weight: bold;'>Hạn chót thanh toán: {vnTime}</p>
                <p><i>Lưu ý: Nếu quá hạn mà chưa nhận được thanh toán, hệ thống sẽ tự động hủy đơn đặt phòng của bạn.</i></p>
                <a href='http://localhost:5173/booking/{bookingId}' style='display:inline-block;background:#0ea5e9;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;margin-top:8px'>Thanh toán ngay →</a>
                <br>
                <p>Cảm ơn bạn đã tin tưởng BookNow!</p>
                <p style='color: #888; font-size: 12px; text-align: center; margin-top: 20px;'>BookNow Team</p>
            </div>
        ";
        return SendAsync(toEmail, subject, html);
    }

    public Task SendDepositReminderAsync(string toEmail, string guestName, string bookingId, decimal depositAmount, DateTime deadline)
    {
        var paymentLink = $"http://localhost:5173/booking/{bookingId}";
        var subject = "⏰ Nhắc nhở: Thanh toán đặt cọc để giữ phòng của bạn";
        var html = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'>
  <div style='background:#0ea5e9;color:white;padding:24px;border-radius:8px 8px 0 0'>
    <h2 style='margin:0'>Nhắc nhở Thanh toán Cọc</h2>
  </div>
  <div style='padding:24px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Xin chào <strong>{guestName}</strong>,</p>
    <p>Đơn đặt phòng <strong>#{bookingId[..8].ToUpper()}</strong> của bạn <strong>chưa được thanh toán cọc</strong>.</p>
    <div style='background:white;border:1px solid #fbbf24;border-radius:8px;padding:16px;margin:16px 0'>
      <p style='margin:0'>💰 Số tiền cọc: <strong style='color:#0ea5e9;font-size:18px'>{depositAmount:N0} VNĐ</strong></p>
      <p style='margin:8px 0 0'>⏰ Hạn thanh toán: <strong style='color:#ef4444'>{deadline.AddHours(7):dd/MM/yyyy HH:mm} (giờ VN)</strong></p>
    </div>
    <p>Vui lòng đăng nhập vào hệ thống và thanh toán cọc để giữ phòng. Nếu quá hạn, đơn sẽ tự động bị hủy.</p>
    <a href='{paymentLink}' style='display:inline-block;background:#0ea5e9;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;margin-top:8px'>Thanh toán ngay →</a>
    <p style='color:#94a3b8;font-size:12px;margin-top:24px'>Email này được gửi tự động từ hệ thống HotelBooking.</p>
  </div>
</div>";
        return SendAsync(toEmail, subject, html);
    }

    public Task SendBookingConfirmedAsync(string toEmail, string guestName, string bookingId)
    {
        var subject = "✅ Đặt phòng đã được xác nhận!";
        var html = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'>
  <div style='background:#22c55e;color:white;padding:24px;border-radius:8px 8px 0 0'>
    <h2 style='margin:0'>✅ Đặt phòng Đã xác nhận</h2>
  </div>
  <div style='padding:24px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Xin chào <strong>{guestName}</strong>,</p>
    <p>Đơn đặt phòng <strong>#{bookingId[..8].ToUpper()}</strong> đã được khách sạn <strong>xác nhận</strong>.</p>
    <p>Hẹn gặp bạn tại khách sạn. Chúc bạn có một chuyến đi tuyệt vời! 🏨</p>
    <p style='color:#94a3b8;font-size:12px;margin-top:24px'>Email này được gửi tự động từ hệ thống HotelBooking.</p>
  </div>
</div>";
        return SendAsync(toEmail, subject, html);
    }

    public Task SendBookingCancelledAsync(string toEmail, string guestName, string bookingId, string reason)
    {
        var subject = "❌ Thông báo: Đơn đặt phòng của bạn đã bị hủy";
        var html = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'>
  <div style='background:#ef4444;color:white;padding:24px;border-radius:8px 8px 0 0'>
    <h2 style='margin:0'>Đơn đặt phòng đã bị hủy</h2>
  </div>
  <div style='padding:24px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Xin chào <strong>{guestName}</strong>,</p>
    <p>Đơn đặt phòng <strong>#{bookingId[..8].ToUpper()}</strong> đã bị hủy.</p>
    <div style='background:#fee2e2;border-radius:8px;padding:16px;margin:16px 0'>
      <p style='margin:0'>Lý do: <strong>{reason}</strong></p>
    </div>
    <p style='color:#94a3b8;font-size:12px;margin-top:24px'>Email này được gửi tự động từ hệ thống HotelBooking.</p>
  </div>
</div>";
        return SendAsync(toEmail, subject, html);
    }

    public Task SendDepositConfirmedAsync(string toEmail, string guestName, string bookingId, decimal depositAmount)
    {
        var subject = "💰 Xác nhận: Đã nhận thanh toán đặt cọc";
        var html = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'>
  <div style='background:#0ea5e9;color:white;padding:24px;border-radius:8px 8px 0 0'>
    <h2 style='margin:0'>✅ Đã nhận tiền cọc</h2>
  </div>
  <div style='padding:24px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Xin chào <strong>{guestName}</strong>,</p>
    <p>Chúng tôi đã nhận được khoản thanh toán cọc cho đơn <strong>#{bookingId[..8].ToUpper()}</strong>.</p>
    <div style='background:#dbeafe;border-radius:8px;padding:16px;margin:16px 0'>
      <p style='margin:0'>💰 Số tiền đã cọc: <strong style='color:#0ea5e9;font-size:18px'>{depositAmount:N0} VNĐ</strong></p>
    </div>
    <p>Đơn của bạn đã được <strong>xác nhận</strong>. Chúc bạn có chuyến đi tuyệt vời! 🏨</p>
    <p style='color:#94a3b8;font-size:12px;margin-top:24px'>Email này được gửi tự động từ hệ thống HotelBooking.</p>
  </div>
</div>";
        return SendAsync(toEmail, subject, html);
    }

    public Task SendDepositRefundedAsync(string toEmail, string guestName, string bookingId, decimal refundAmount)
    {
        var subject = "💸 Thông báo: Đã hoàn cọc cho bạn";
        var html = $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'>
  <div style='background:#8b5cf6;color:white;padding:24px;border-radius:8px 8px 0 0'>
    <h2 style='margin:0'>💸 Thông báo Hoàn Cọc</h2>
  </div>
  <div style='padding:24px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Xin chào <strong>{guestName}</strong>,</p>
    <p>Chúng tôi đã xử lý hoàn trả tiền cọc cho đơn <strong>#{bookingId[..8].ToUpper()}</strong>.</p>
    <div style='background:#ede9fe;border-radius:8px;padding:16px;margin:16px 0'>
      <p style='margin:0'>💰 Số tiền hoàn: <strong style='color:#8b5cf6;font-size:18px'>{refundAmount:N0} VNĐ</strong></p>
    </div>
    <p>Số tiền sẽ được chuyển về tài khoản trong vòng <strong>3-5 ngày làm việc</strong>.</p>
    <p style='color:#94a3b8;font-size:12px;margin-top:24px'>Email này được gửi tự động từ hệ thống HotelBooking.</p>
  </div>
</div>";
        return SendAsync(toEmail, subject, html);
    }
    public Task SendPostCheckoutThankYouAsync(string toEmail, string guestName, string bookingId, string hotelName, DateOnly checkOutDate)
    {
        var subject = string.Format("[BookNow] Cam on ban da luu tru tai {0}!", hotelName);
        var checkOutStr = checkOutDate.ToString("dd/MM/yyyy");
        var shortId = bookingId.Length >= 8 ? bookingId.Substring(0, 8).ToUpper() : bookingId.ToUpper();
        var html = string.Format(@"<div style='font-family:Arial,sans-serif;max-width:600px;margin:auto'><div style='background:linear-gradient(135deg,#6366f1,#8b5cf6);color:white;padding:32px;border-radius:12px 12px 0 0;text-align:center'><h2 style='margin:0'>Cam on ban da luu tru!</h2><p style='margin:8px 0 0;opacity:0.85;font-size:14px'>Hy vong ban co mot ky nghi tuyet voi</p></div><div style='padding:28px;background:#f8fafc;border:1px solid #e2e8f0;border-radius:0 0 12px 12px'><p>Xin chao <strong>{0}</strong>,</p><p>Cam on ban da tin tuong lua chon <strong>{1}</strong> lam noi luu tru (Don <strong>#{2}</strong>).</p><div style='background:white;border:1px solid #e2e8f0;border-radius:8px;padding:20px;margin:16px 0;text-align:center'><p style='margin:0;color:#6b7280;font-size:13px'>Ngay tra phong</p><p style='margin:4px 0 0;font-size:18px;font-weight:bold'>{3}</p></div><p>Chung toi mong duoc don tiep ban trong nhung chuyen di tiep theo!</p><div style='text-align:center;margin-top:24px'><a href='http://localhost:5173/my-bookings' style='display:inline-block;background:#6366f1;color:white;padding:12px 28px;border-radius:8px;text-decoration:none;font-weight:bold'>Xem lich su dat phong</a></div><p style='color:#94a3b8;font-size:12px;margin-top:24px;text-align:center'>Email nay duoc gui tu dong tu BookNow.</p></div></div>", guestName, hotelName, shortId, checkOutStr);
        return SendAsync(toEmail, subject, html);
    }
}
