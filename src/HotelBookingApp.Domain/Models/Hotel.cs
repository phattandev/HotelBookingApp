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

    [Column("is_active")]
    public bool IsActive { get; set; } = false;

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
}
