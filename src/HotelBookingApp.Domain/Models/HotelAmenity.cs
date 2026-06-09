using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Infrastructure;

[Table("hotel_amenities")]
[PrimaryKey(nameof(HotelId), nameof(AmenityId))] // Khai báo Khóa phức hợp (EF Core 7+)
public class HotelAmenity
{
    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    [Column("amenity_id")]
    public Guid AmenityId { get; set; }

    [Column("added_at")]
    public DateTime? AddedAt { get; set; }

    [ForeignKey(nameof(HotelId))]
    public virtual Hotel Hotel { get; set; } = null!;

    [ForeignKey(nameof(AmenityId))]
    public virtual Amenity Amenity { get; set; } = null!;
}