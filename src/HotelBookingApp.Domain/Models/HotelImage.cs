using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models;

[Table("hotel_images")]
public class HotelImage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    [Required]
    [Column("url", TypeName = "text")]
    public string Url { get; set; } = null!;

    [Required]
    [Column("public_id")]
    [MaxLength(255)]
    public string PublicId { get; set; } = null!; // Cloudinary public_id (dùng để xóa)

    [Column("is_primary")]
    public bool IsPrimary { get; set; } = false;

    [Column("display_order")]
    public int DisplayOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(HotelId))]
    public virtual Hotel Hotel { get; set; } = null!;
}
