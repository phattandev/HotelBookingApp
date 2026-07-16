using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Domain.Models;

/// <summary>
/// Chính sách hủy phòng của khách sạn.
/// Mỗi khách sạn có đúng 1 chính sách hủy (Unique trên HotelId).
/// Nếu khách hủy đơn trước <see cref="HoursBeforeCheckIn"/> giờ → hoàn 100%.
/// Nếu hủy SAU mốc đó → bị phạt theo <see cref="PenaltyPercentage"/>%.
/// </summary>
[Table("hotel_cancellation_policies")]
[Index(nameof(HotelId), IsUnique = true, Name = "idx_cancellation_policies_hotel_id")]
public class HotelCancellationPolicy
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>Khách sạn áp dụng chính sách này.</summary>
    [Required]
    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    /// <summary>Tên chính sách mô tả ngắn gọn. VD: "Linh hoạt 24h", "Tiêu chuẩn 48h".</summary>
    [Required]
    [MaxLength(100)]
    [Column("policy_name")]
    public string PolicyName { get; set; } = null!;

    /// <summary>
    /// Mốc số giờ trước giờ check-in.
    /// Nếu khách hủy TRƯỚC mốc này → hoàn tiền 100%.
    /// Nếu hủy SAU mốc này → áp dụng phạt <see cref="PenaltyPercentage"/>%.
    /// VD: 24 = nếu hủy trước 24h thì hoàn tiền, hủy trong 24h cuối thì bị phạt.
    /// </summary>
    [Required]
    [Column("hours_before_check_in")]
    public int HoursBeforeCheckIn { get; set; }

    /// <summary>
    /// Tỉ lệ phạt (%) áp dụng khi khách hủy SAU mốc <see cref="HoursBeforeCheckIn"/>.
    /// VD: 100 = mất toàn bộ tiền. 50 = mất 50%, hoàn 50%.
    /// </summary>
    [Required]
    [Column("penalty_percentage", TypeName = "decimal(5,2)")]
    public decimal PenaltyPercentage { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation Properties ---
    [ForeignKey(nameof(HotelId))]
    public virtual Hotel Hotel { get; set; } = null!;
}
