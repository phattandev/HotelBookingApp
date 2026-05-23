using System;
using HotelBookingApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; set; }
        DbSet<Amenity> Amenities { get; set; }

        DbSet<AmenityCategory> AmenityCategories { get; set; }

        DbSet<Hotel> Hotels { get; set; }

        DbSet<HotelAmenity> HotelAmenities { get; set; }

        DbSet<Province> Provinces { get; set; }

        DbSet<RoomType> RoomTypes { get; set; }

        DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }

        DbSet<Ward> Wards { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
