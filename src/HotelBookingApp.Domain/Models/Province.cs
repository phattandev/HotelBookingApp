using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class Province
{
    public short Id { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string Type { get; set; } = null!;

    public bool? IsActive { get; set; }

    public virtual ICollection<Ward> Wards { get; set; } = new List<Ward>();
}
