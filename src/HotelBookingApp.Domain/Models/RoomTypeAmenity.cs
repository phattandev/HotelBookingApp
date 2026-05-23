using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class RoomTypeAmenity
{
    public long RoomTypeId { get; set; }

    public int AmenityId { get; set; }

    public short? Quantity { get; set; }

    public virtual Amenity Amenity { get; set; } = null!;

    public virtual RoomType RoomType { get; set; } = null!;
}
