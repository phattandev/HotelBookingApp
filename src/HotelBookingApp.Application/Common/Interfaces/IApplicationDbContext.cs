using System;
using HotelBookingApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<User> Users { get; set; }
        DbSet<Role> Roles { get; set; }
        DbSet<Business> Businesses { get; set; }
        DbSet<HotelStaffAssignment> HotelStaffAssignments { get; set; }
        DbSet<Hotel> Hotels { get; set; }
        DbSet<RoomType> RoomTypes { get; set; }
        DbSet<AmenityCategory> AmenityCategories { get; set; }
        DbSet<Amenity> Amenities { get; set; }
        DbSet<HotelAmenity> HotelAmenities { get; set; }
        DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }
        DbSet<Province> Provinces { get; set; }
        DbSet<Ward> Wards { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
