using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models;

[Table("room_type_images")]
public class RoomTypeImage
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("room_type_id")]
    public Guid RoomTypeId { get; set; }

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

    [ForeignKey(nameof(RoomTypeId))]
    public virtual RoomType RoomType { get; set; } = null!;
}
