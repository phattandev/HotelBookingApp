using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Domain.Models;

[Table("room_type_amenities")]
[PrimaryKey(nameof(RoomTypeId), nameof(AmenityId))] // Khai báo Khóa phức hợp (EF Core 7+)
public class RoomTypeAmenity
{
    [Column("room_type_id")]
    public Guid RoomTypeId { get; set; }

    [Column("amenity_id")]
    public Guid AmenityId { get; set; }

    [ForeignKey(nameof(RoomTypeId))]
    public virtual RoomType RoomType { get; set; } = null!;

    [ForeignKey(nameof(AmenityId))]
    public virtual Amenity Amenity { get; set; } = null!;
}