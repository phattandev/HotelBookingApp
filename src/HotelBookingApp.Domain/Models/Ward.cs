using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class Ward
{
    public int Id { get; set; }

    public short ProvinceId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();

    public virtual Province Province { get; set; } = null!;
}
