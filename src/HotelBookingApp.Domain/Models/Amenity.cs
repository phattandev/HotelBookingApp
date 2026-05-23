using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class Amenity
{
    public int Id { get; set; }

    public short CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? IconUrl { get; set; }

    public bool? IsActive { get; set; }

    public virtual AmenityCategory Category { get; set; } = null!;

    public virtual ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();

    public virtual ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
}
