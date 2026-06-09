using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelBookingApp.Infrastructure;

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
        [MaxLength(20)]
        [Column("verification_status")]
        public string VerificationStatus { get; set; } = "Pending";

        // --- Navigation Properties ---
        [ForeignKey(nameof(OwnerId))]
        public virtual User Owner { get; set; } = null!;

        [InverseProperty(nameof(User.Business))]
        public virtual ICollection<User> Employees { get; set; } = new List<User>();

        [InverseProperty(nameof(Hotel.Business))]
        public virtual ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
    }
}

