using System;
using System.Collections.Generic;

namespace HotelBookingApp.Infrastructure;

public partial class User
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }

    public string? AvatarUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
