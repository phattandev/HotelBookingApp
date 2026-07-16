using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Domain.Models;

/// <summary>
/// Chính sách đặt cọc của khách sạn.
/// Mỗi khách sạn có đúng 1 chính sách đặt cọc (Unique trên HotelId).
/// </summary>
[Table("hotel_deposit_policies")]
[Index(nameof(HotelId), IsUnique = true, Name = "idx_deposit_policies_hotel_id")]
public class HotelDepositPolicy
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>Khách sạn áp dụng chính sách này.</summary>
    [Required]
    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    /// <summary>
    /// Số giờ tối thiểu trước ngày check-in (00:00) mà khách phải thanh toán cọc.
    /// Ví dụ: 24h nghĩa là phải cọc trước 1 ngày so với ngày check-in.
    /// Nếu đặt phòng sát giờ (nhỏ hơn mốc này), cọc phải được thanh toán ngay sau khi duyệt.
    /// </summary>
    [Required]
    [Column("hours_before_check_in")]
    public int HoursBeforeCheckIn { get; set; }

    /// <summary>
    /// Tỷ lệ cọc (%). Mặc định 50%.
    /// </summary>
    [Required]
    [Column("deposit_percentage", TypeName = "decimal(5,2)")]
    public decimal DepositPercentage { get; set; } = 50.00m;

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
