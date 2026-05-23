using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class RoomType
{
    public long Id { get; set; }

    public long HotelId { get; set; }

    public string Name { get; set; } = null!;

    public virtual Hotel Hotel { get; set; } = null!;

    public virtual ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();
}
