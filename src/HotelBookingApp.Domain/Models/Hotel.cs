using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class Hotel
{
    public long Id { get; set; }

    public int WardId { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();

    public virtual ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    public virtual Ward Ward { get; set; } = null!;
}
