using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Domain.Models;

/// <summary>
/// Đánh giá của khách hàng sau khi checkout.
/// Chỉ tạo được khi đơn đặt phòng có trạng thái Completed.
/// Mỗi đơn chỉ có duy nhất 1 đánh giá.
/// </summary>
[Table("reviews")]
[Index(nameof(BookingId), IsUnique = true, Name = "idx_reviews_booking_id")]
public class Review
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>Đơn đặt phòng tương ứng (1-1, một đơn chỉ có 1 review).</summary>
    [Required]
    [Column("booking_id")]
    public Guid BookingId { get; set; }

    /// <summary>Khách sạn được đánh giá.</summary>
    [Required]
    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    /// <summary>Khách hàng viết đánh giá.</summary>
    [Required]
    [Column("customer_id")]
    public Guid CustomerId { get; set; }

    // --- Các tiêu chí đánh giá (1-10, nullable = không đánh giá tiêu chí đó) ---

    /// <summary>Không gian (1-10)</summary>
    [Column("score_space")]
    public int? ScoreSpace { get; set; }

    /// <summary>Dịch vụ (1-10)</summary>
    [Column("score_service")]
    public int? ScoreService { get; set; }

    /// <summary>Trải nghiệm tổng thể (1-10)</summary>
    [Column("score_experience")]
    public int? ScoreExperience { get; set; }

    /// <summary>Độ an toàn (1-10)</summary>
    [Column("score_safety")]
    public int? ScoreSafety { get; set; }

    /// <summary>Vệ sinh sạch sẽ (1-10)</summary>
    [Column("score_cleanliness")]
    public int? ScoreCleanliness { get; set; }

    /// <summary>View & vị trí (1-10)</summary>
    [Column("score_view")]
    public int? ScoreView { get; set; }

    /// <summary>Chất lượng phòng (1-10)</summary>
    [Column("score_room_quality")]
    public int? ScoreRoomQuality { get; set; }

    /// <summary>Ẩm thực (1-10)</summary>
    [Column("score_food")]
    public int? ScoreFood { get; set; }

    /// <summary>Yên tĩnh (1-10)</summary>
    [Column("score_quietness")]
    public int? ScoreQuietness { get; set; }

    /// <summary>Sự thân thiện của nhân viên (1-10)</summary>
    [Column("score_staff_friendliness")]
    public int? ScoreStaffFriendliness { get; set; }

    /// <summary>Đánh giá bằng văn bản tự do.</summary>
    [Column("comment", TypeName = "text")]
    public string? Comment { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation Properties ---
    [ForeignKey(nameof(BookingId))]
    public virtual Booking Booking { get; set; } = null!;

    [ForeignKey(nameof(HotelId))]
    public virtual Hotel Hotel { get; set; } = null!;

    [ForeignKey(nameof(CustomerId))]
    public virtual User Customer { get; set; } = null!;
}
