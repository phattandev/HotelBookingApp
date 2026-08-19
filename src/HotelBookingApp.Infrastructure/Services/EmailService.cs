using System.Linq;
using System.Net;
using System.Net.Mail;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Domain.Models;
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

    private string GetBaseEmailTemplate(string title, string content, string ctaText, string ctaLink, string colorHex)
    {
        return $@"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; background-color: #f4f4f5; padding: 40px 20px;'>
    <div style='background-color: white; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);'>
        <!-- Header -->
        <div style='background-color: {colorHex}; padding: 30px; text-align: center; color: white;'>
            <h1 style='margin: 0; font-size: 24px;'>{title}</h1>
        </div>
        
        <!-- Body -->
        <div style='padding: 30px; color: #3f3f46; font-size: 16px; line-height: 1.6;'>
            {content}
            
            <!-- Call to Action -->
            <div style='text-align: center; margin-top: 30px;'>
                <a href='{ctaLink}' style='display: inline-block; background-color: {colorHex}; color: white; padding: 14px 28px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px;'>{ctaText}</a>
            </div>
        </div>
        
        <!-- Footer -->
        <div style='background-color: #f8fafc; padding: 20px; text-align: center; font-size: 14px; color: #94a3b8; border-top: 1px solid #e2e8f0;'>
            <p style='margin: 0;'>Email này được gửi tự động từ hệ thống HotelBooking.</p>
            <p style='margin: 4px 0 0;'>Vui lòng không trả lời email này.</p>
        </div>
    </div>
</div>";
    }

    private string GetBookingDetailsHtml(Booking b)
    {
        var roomNames = b.Items != null && b.Items.Any() ? string.Join(", ", b.Items.Select(i => $"{i.NumRooms}x {i.RoomType?.Name}")) : "N/A";
        return $@"
        <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 20px; margin: 20px 0;'>
            <h3 style='margin-top: 0; color: #1e293b; font-size: 16px; border-bottom: 1px solid #e2e8f0; padding-bottom: 10px;'>Chi tiết đơn đặt phòng #{b.Id.ToString()[..8].ToUpper()}</h3>
            <table style='width: 100%; font-size: 14px;'>
                <tr><td style='padding: 6px 0; color: #64748b;'>Khách sạn:</td><td style='padding: 6px 0; font-weight: 500; text-align: right;'>{b.Hotel?.Name}</td></tr>
                <tr><td style='padding: 6px 0; color: #64748b;'>Khách hàng:</td><td style='padding: 6px 0; font-weight: 500; text-align: right;'>{b.GuestName}</td></tr>
                <tr><td style='padding: 6px 0; color: #64748b;'>Nhận phòng:</td><td style='padding: 6px 0; font-weight: 500; text-align: right;'>{b.CheckInDate:dd/MM/yyyy}</td></tr>
                <tr><td style='padding: 6px 0; color: #64748b;'>Trả phòng:</td><td style='padding: 6px 0; font-weight: 500; text-align: right;'>{b.CheckOutDate:dd/MM/yyyy}</td></tr>
                <tr><td style='padding: 6px 0; color: #64748b;'>Số khách:</td><td style='padding: 6px 0; font-weight: 500; text-align: right;'>{b.NumAdults} Người lớn{(b.NumChildren > 0 ? $", {b.NumChildren} Trẻ em" : "")}</td></tr>
                <tr><td style='padding: 6px 0; color: #64748b;'>Phòng:</td><td style='padding: 6px 0; font-weight: 500; text-align: right;'>{roomNames}</td></tr>
                <tr><td style='padding: 6px 0; color: #64748b; border-top: 1px dashed #cbd5e1;'>Tổng tiền:</td><td style='padding: 6px 0; font-weight: bold; color: #0f172a; border-top: 1px dashed #cbd5e1; text-align: right;'>{b.TotalPrice:N0} VNĐ</td></tr>
            </table>
        </div>";
    }

    public Task SendBookingApprovedAsync(Booking booking)
    {
        string subject = $"[BookNow] Đơn #{booking.Id.ToString()[..8].ToUpper()} đã duyệt - Yêu cầu cọc";
        string vnTime = booking.DepositDeadline?.AddHours(7).ToString("dd/MM/yyyy HH:mm") ?? "";
        string content = $@"
            <p>Xin chào <strong>{booking.GuestName}</strong>,</p>
            <p>Khách sạn <strong>{booking.Hotel?.Name}</strong> đã duyệt yêu cầu đặt phòng của bạn.</p>
            {GetBookingDetailsHtml(booking)}
            <div style='background-color: #f0f9ff; border-left: 4px solid #0ea5e9; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0;'>Số tiền cần cọc: <strong style='color: #0ea5e9; font-size: 18px;'>{booking.DepositAmount:N0} VNĐ</strong></p>
                <p style='margin: 8px 0 0; color: #dc2626;'>Hạn chót thanh toán: <strong>{vnTime}</strong> (giờ VN)</p>
            </div>
            <p>Vui lòng thanh toán cọc để giữ phòng. Nếu quá hạn, đơn sẽ tự động bị hủy.</p>
        ";
        string html = GetBaseEmailTemplate("Đơn Đã Được Duyệt!", content, "Thanh toán ngay", $"http://localhost:5173/my-bookings", "#0ea5e9");
        return SendAsync(booking.GuestEmail, subject, html);
    }

    public Task SendDepositReminderAsync(Booking booking)
    {
        string subject = $"⏰ Nhắc nhở cọc đơn #{booking.Id.ToString()[..8].ToUpper()}";
        string vnTime = booking.DepositDeadline?.AddHours(7).ToString("dd/MM/yyyy HH:mm") ?? "";
        string content = $@"
            <p>Xin chào <strong>{booking.GuestName}</strong>,</p>
            <p>Đơn đặt phòng của bạn tại <strong>{booking.Hotel?.Name}</strong> sắp hết hạn đặt cọc.</p>
            {GetBookingDetailsHtml(booking)}
            <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0;'>Số tiền cần cọc: <strong style='color: #f59e0b; font-size: 18px;'>{booking.DepositAmount:N0} VNĐ</strong></p>
                <p style='margin: 8px 0 0; color: #dc2626;'>Hạn chót: <strong>{vnTime}</strong> (giờ VN)</p>
            </div>
            <p>Xin lưu ý, hệ thống sẽ tự động hủy đơn sau thời gian trên nếu chưa nhận được thanh toán.</p>
        ";
        string html = GetBaseEmailTemplate("Nhắc Nhở Thanh Toán", content, "Thanh toán ngay", $"http://localhost:5173/my-bookings", "#f59e0b");
        return SendAsync(booking.GuestEmail, subject, html);
    }

    public Task SendBookingConfirmedAsync(Booking booking)
    {
        string subject = $"✅ Xác nhận đặt phòng #{booking.Id.ToString()[..8].ToUpper()}";
        string content = $@"
            <p>Xin chào <strong>{booking.GuestName}</strong>,</p>
            <p>Đơn đặt phòng của bạn đã được <strong>xác nhận thành công</strong>.</p>
            {GetBookingDetailsHtml(booking)}
            <p>Hẹn gặp bạn tại khách sạn. Chúc bạn có một chuyến đi tuyệt vời! 🏨</p>
        ";
        string html = GetBaseEmailTemplate("Xác Nhận Đặt Phòng", content, "Xem chi tiết", $"http://localhost:5173/my-bookings", "#22c55e");
        return SendAsync(booking.GuestEmail, subject, html);
    }

    public Task SendBookingCancelledAsync(Booking booking)
    {
        string subject = $"❌ Hủy đơn #{booking.Id.ToString()[..8].ToUpper()}";
        string content = $@"
            <p>Xin chào <strong>{booking.GuestName}</strong>,</p>
            <p>Đơn đặt phòng của bạn tại <strong>{booking.Hotel?.Name}</strong> đã bị hủy.</p>
            <div style='background-color: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0; color: #991b1b;'><strong>Lý do hủy:</strong> {booking.CancelReason}</p>
            </div>
            {GetBookingDetailsHtml(booking)}
            <p>Nếu bạn có thắc mắc, vui lòng liên hệ bộ phận hỗ trợ.</p>
        ";
        string html = GetBaseEmailTemplate("Đơn Đã Bị Hủy", content, "Xem lịch sử", $"http://localhost:5173/my-bookings", "#ef4444");
        return SendAsync(booking.GuestEmail, subject, html);
    }

    public Task SendDepositConfirmedAsync(Booking booking)
    {
        string subject = $"💰 Xác nhận đã nhận cọc #{booking.Id.ToString()[..8].ToUpper()}";
        string content = $@"
            <p>Xin chào <strong>{booking.GuestName}</strong>,</p>
            <p>Chúng tôi đã nhận được thanh toán cọc cho đơn đặt phòng của bạn.</p>
            <div style='background-color: #f0fdf4; border-left: 4px solid #22c55e; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0; color: #166534;'>Số tiền đã nhận: <strong>{booking.DepositAmount:N0} VNĐ</strong></p>
            </div>
            {GetBookingDetailsHtml(booking)}
            <p>Đơn phòng của bạn hiện đã được <strong>Xác nhận</strong> an toàn.</p>
        ";
        string html = GetBaseEmailTemplate("Đã Nhận Tiền Cọc", content, "Xem chi tiết", $"http://localhost:5173/my-bookings", "#22c55e");
        return SendAsync(booking.GuestEmail, subject, html);
    }

    public Task SendDepositRefundedAsync(Booking booking)
    {
        string subject = $"💸 Hoàn cọc đơn #{booking.Id.ToString()[..8].ToUpper()}";
        string content = $@"
            <p>Xin chào <strong>{booking.GuestName}</strong>,</p>
            <p>Yêu cầu hoàn trả tiền cọc của bạn đã được xử lý thành công.</p>
            <div style='background-color: #faf5ff; border-left: 4px solid #a855f7; padding: 15px; margin: 20px 0;'>
                <p style='margin: 0; color: #6b21a8;'>Số tiền hoàn trả: <strong>{booking.RefundAmount?.ToString("N0") ?? "0"} VNĐ</strong></p>
                <p style='margin: 8px 0 0; font-size: 14px;'>Tiền sẽ về tài khoản trong 3-5 ngày làm việc.</p>
            </div>
            {GetBookingDetailsHtml(booking)}
        ";
        string html = GetBaseEmailTemplate("Hoàn Cọc Thành Công", content, "Xem lịch sử", $"http://localhost:5173/my-bookings", "#a855f7");
        return SendAsync(booking.GuestEmail, subject, html);
    }

    public Task SendPostCheckoutThankYouAsync(Booking booking)
    {
        string subject = $"[BookNow] Cảm ơn bạn đã lưu trú tại {booking.Hotel?.Name}!";
        string content = $@"
            <p>Xin chào <strong>{booking.GuestName}</strong>,</p>
            <p>Cảm ơn bạn đã tin tưởng lựa chọn <strong>{booking.Hotel?.Name}</strong> làm nơi lưu trú.</p>
            {GetBookingDetailsHtml(booking)}
            <p>Chúng tôi hy vọng bạn đã có một kỳ nghỉ tuyệt vời!</p>
            <p>Để giúp chúng tôi phục vụ tốt hơn trong tương lai, cũng như chia sẻ trải nghiệm của bạn với các du khách khác, <strong>xin bớt chút thời gian để lại đánh giá về khách sạn nhé.</strong></p>
        ";
        string html = GetBaseEmailTemplate("Cảm Ơn Quý Khách", content, "Đánh giá ngay", $"http://localhost:5173/my-bookings", "#6366f1");
        return SendAsync(booking.GuestEmail, subject, html);
    }
}
