using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelBookingApp.Domain.Models;

namespace HotelBookingApp.Domain.Models
{
    [Table("businesses")]
    public class Business
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("owner_id")]
        public Guid OwnerId { get; set; }

        [Required]
        [MaxLength(200)]
        [Column("business_name")]
        public string BusinessName { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        [Column("tax_code")]
        public string TaxCode { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        [Column("business_address")]
        public string BusinessAddress { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Column("representative_name")]
        public string RepresentativeName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        [Column("position")]
        public string Position { get; set; } = null!;

        [Required]
        [Column("verification_status")]
        public BusinessVerificationStatus VerificationStatus { get; set; } = BusinessVerificationStatus.Pending;

        /// <summary>Lý do Admin từ chối hồ sơ doanh nghiệp (nếu có).</summary>
        [Column("rejection_reason", TypeName = "text")]
        public string? RejectionReason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // --- Navigation Properties ---
        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;

        [InverseProperty(nameof(BusinessStaff.Business))]
        public virtual ICollection<BusinessStaff> Staff { get; set; } = new List<BusinessStaff>();

        [InverseProperty(nameof(Hotel.Business))]
        public virtual ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
    }
}

