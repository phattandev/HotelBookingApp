using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class AmenityCategory
{
    public short Id { get; set; }

    public string Name { get; set; } = null!;

    public string TargetType { get; set; } = null!;

    public short? DisplayOrder { get; set; }

    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
}
