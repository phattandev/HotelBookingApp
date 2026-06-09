using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Infrastructure;

[Table("amenities")]
public class Amenity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [ForeignKey(nameof(CategoryId))]
    public virtual AmenityCategory Category { get; set; } = null!;

    [InverseProperty(nameof(HotelAmenity.Amenity))]
    public virtual ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();

    [InverseProperty(nameof(RoomTypeAmenity.Amenity))]
    public virtual ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
}