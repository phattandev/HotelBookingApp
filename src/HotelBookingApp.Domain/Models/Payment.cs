using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models;

/// <summary>
/// Lưu trữ lịch sử giao dịch thanh toán qua cổng thanh toán (ví dụ: VNPAY).
/// </summary>
[Table("payments")]
public class Payment
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("booking_id")]
    public Guid BookingId { get; set; }

    [Required]
    [Column("amount", TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    [Column("currency")]
    public string Currency { get; set; } = "VND";

    /// <summary>Mã giao dịch trả về từ cổng thanh toán (VD: vnp_TransactionNo)</summary>
    [MaxLength(100)]
    [Column("transaction_id")]
    public string? TransactionId { get; set; }

    /// <summary>Mã đơn hàng gửi sang cổng thanh toán (VD: vnp_TxnRef)</summary>
    [MaxLength(100)]
    [Column("order_reference")]
    public string? OrderReference { get; set; }

    [MaxLength(50)]
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "VNPAY";

    [Required]
    [Column("status")]
    public PaymentTransactionStatus Status { get; set; } = PaymentTransactionStatus.Pending;

    /// <summary>Mã lỗi từ cổng thanh toán (VD: 00 là thành công)</summary>
    [MaxLength(20)]
    [Column("response_code")]
    public string? ResponseCode { get; set; }

    [Required]
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(BookingId))]
    public virtual Booking Booking { get; set; } = null!;
}
