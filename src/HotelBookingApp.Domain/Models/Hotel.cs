using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HotelBookingApp.Domain.Models;

namespace HotelBookingApp.Domain.Models;

[Table("hotels")]
public class Hotel
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("business_id")]
    public Guid BusinessId { get; set; }

    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(255)]
    [Column("address_line")]
    public string AddressLine { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    [Column("tax_code")]
    public string TaxCode { get; set; } = null!;

    [Required]
    [Column("ward_id")]
    public Guid WardId { get; set; }

    [Required]
    [MaxLength(20)]
    [Column("approval_status")]
    public HotelApprovalStatus ApprovalStatus { get; set; } = HotelApprovalStatus.Pending;

    /// <summary>Lý do Admin từ chối duyệt khách sạn (nếu có).</summary>
    [Column("rejection_reason", TypeName = "text")]
    public string? RejectionReason { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = false;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("star_rating")]
    public int? StarRating { get; set; } // 1-5, nullable

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // --- Navigation Properties ---
    [ForeignKey(nameof(BusinessId))]
    public virtual Business Business { get; set; } = null!;

    [ForeignKey(nameof(WardId))]
    public virtual Ward Ward { get; set; } = null!;

    [InverseProperty(nameof(HotelStaffAssignment.Hotel))]
    public virtual ICollection<HotelStaffAssignment> StaffAssignments { get; set; } = new List<HotelStaffAssignment>();

    [InverseProperty(nameof(RoomType.Hotel))]
    public virtual ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    [InverseProperty(nameof(HotelAmenity.Hotel))]
    public virtual ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();

    [InverseProperty(nameof(HotelImage.Hotel))]
    public virtual ICollection<HotelImage> Images { get; set; } = new List<HotelImage>();

    [InverseProperty(nameof(HotelCancellationPolicy.Hotel))]
    public virtual HotelCancellationPolicy? CancellationPolicy { get; set; }

    [InverseProperty(nameof(HotelDepositPolicy.Hotel))]
    public virtual HotelDepositPolicy? DepositPolicy { get; set; }

    [InverseProperty(nameof(Booking.Hotel))]
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
