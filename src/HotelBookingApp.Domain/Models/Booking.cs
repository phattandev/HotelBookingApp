using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models;

/// <summary>
/// Đơn đặt phòng của khách hàng.
/// Lưu toàn bộ thông tin giao dịch: loại phòng, ngày, giá, trạng thái và lý do hủy nếu có.
/// </summary>
[Table("bookings")]
public class Booking
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>Loại phòng được đặt.</summary>
    [Required]
    [Column("room_type_id")]
    public Guid RoomTypeId { get; set; }

    /// <summary>Khách hàng đặt phòng (FK đến User).</summary>
    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    /// <summary>Ngày nhận phòng (chỉ lấy phần ngày, không có giờ).</summary>
    [Required]
    [Column("check_in_date", TypeName = "date")]
    public DateOnly CheckInDate { get; set; }

    /// <summary>Ngày trả phòng (chỉ lấy phần ngày, không có giờ).</summary>
    [Required]
    [Column("check_out_date", TypeName = "date")]
    public DateOnly CheckOutDate { get; set; }

    /// <summary>Số phòng cùng loại được đặt trong một lần.</summary>
    [Required]
    [Column("num_rooms")]
    public int NumRooms { get; set; } = 1;

    [Required]
    [Column("num_adults")]
    public int NumAdults { get; set; } = 1;

    [Required]
    [Column("num_children")]
    public int NumChildren { get; set; } = 0;

    [Required]
    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    /// <summary>Tổng tiền đã tính = BasePrice x NumRooms x số đêm.</summary>
    [Required]
    [Column("total_price", TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }

    /// <summary>Trạng thái đơn: Pending, Confirmed, Cancelled, Completed.</summary>
    [Required]
    [Column("status")]
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    /// <summary>Tên người đặt (có thể khác chủ tài khoản).</summary>
    [Required]
    [MaxLength(150)]
    [Column("guest_name")]
    public string GuestName { get; set; } = null!;

    /// <summary>Số điện thoại liên hệ của người đặt.</summary>
    [Required]
    [MaxLength(20)]
    [Column("guest_phone")]
    public string GuestPhone { get; set; } = null!;

    /// <summary>Email liên hệ của người đặt.</summary>
    [Required]
    [MaxLength(200)]
    [Column("guest_email")]
    public string GuestEmail { get; set; } = null!;

    /// <summary>Yêu cầu đặc biệt (không bắt buộc).</summary>
    [Column("special_requests", TypeName = "text")]
    public string? SpecialRequests { get; set; }

    /// <summary>
    /// Lý do hủy đơn. Bắt buộc khi Status = Cancelled.
    /// Được điền bởi khách hàng (tự hủy) hoặc Manager (từ chối đơn).
    /// </summary>
    [Column("cancel_reason", TypeName = "text")]
    public string? CancelReason { get; set; }

    /// <summary>
    /// ID chính sách hủy phòng được áp dụng tại thời điểm đặt phòng (snapshot).
    /// Lưu lại để tránh tranh chấp nếu Partner thay đổi chính sách sau khi khách đã đặt.
    /// </summary>
    [Column("cancellation_policy_id")]
    public Guid? CancellationPolicyId { get; set; }

    [Column("penalty_percentage_snapshot", TypeName = "decimal(5,2)")]
    public decimal? PenaltyPercentageSnapshot { get; set; }

    [Column("deposit_policy_id")]
    public Guid? DepositPolicyId { get; set; }

    [Column("deposit_percentage_snapshot", TypeName = "decimal(5,2)")]
    public decimal? DepositPercentageSnapshot { get; set; }

    /// <summary>Số tiền bị phạt khi hủy (tính dựa theo PenaltyPercentage của policy).</summary>
    [Column("penalty_amount", TypeName = "decimal(18,2)")]
    public decimal? PenaltyAmount { get; set; }

    /// <summary>Số tiền hoàn trả khách = TotalPrice - PenaltyAmount.</summary>
    [Column("refund_amount", TypeName = "decimal(18,2)")]
    public decimal? RefundAmount { get; set; }

    /// <summary>Thời điểm chính xác lúc khách hủy đơn (dùng để tính giờ còn lại).</summary>
    [Column("cancelled_at")]
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Trạng thái thanh toán tiền cọc.
    /// Unpaid: Chưa cọc. Paid: Đã cọc. Refunded: Đã hoàn cọc.
    /// </summary>
    [Required]
    [Column("payment_status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    /// <summary>Số tiền cần đặt cọc = TotalPrice * 50%.</summary>
    [Column("deposit_amount", TypeName = "decimal(18,2)")]
    public decimal DepositAmount { get; set; }

    /// <summary>
    /// Thời hạn thanh toán cọc. 
    /// Được set khi Quản lý duyệt đơn đặt phòng.
    /// Quá hạn mà chưa cọc → hệ thống tự hủy đơn.
    /// </summary>
    [Column("deposit_deadline")]
    public DateTime? DepositDeadline { get; set; }

    /// <summary>Thời điểm khách thanh toán cọc thành công.</summary>
    [Column("paid_at")]
    public DateTime? PaidAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation Properties ---
    [ForeignKey(nameof(RoomTypeId))]
    public virtual RoomType RoomType { get; set; } = null!;

    [ForeignKey(nameof(CustomerId))]
    public virtual User Customer { get; set; } = null!;

    [ForeignKey(nameof(HotelId))]
    public virtual Hotel Hotel { get; set; } = null!;
}
