using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class HotelAmenity
{
    public long HotelId { get; set; }

    public int AmenityId { get; set; }

    public DateTime? AddedAt { get; set; }

    public virtual Amenity Amenity { get; set; } = null!;

    public virtual Hotel Hotel { get; set; } = null!;
}
