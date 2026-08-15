using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models
{
    [Table("business_documents")]
    public class BusinessDocument
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("business_id")]
        public Guid BusinessId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("file_name")]
        public string FileName { get; set; } = null!;

        [Required]
        [Column("file_url")]
        public string FileUrl { get; set; } = null!;

        [Required]
        [Column("public_id")]
        public string PublicId { get; set; } = null!;

        [Required]
        [Column("file_size_bytes")]
        public long FileSizeBytes { get; set; }

        [Column("uploaded_at")]
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        [ForeignKey(nameof(BusinessId))]
        public virtual Business Business { get; set; } = null!;
    }
}
