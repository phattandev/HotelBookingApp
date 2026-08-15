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
        DbSet<BusinessDocument> BusinessDocuments { get; set; }
        DbSet<BusinessStaff> BusinessStaff { get; set; }
        DbSet<HotelStaffAssignment> HotelStaffAssignments { get; set; }
        DbSet<Hotel> Hotels { get; set; }
        DbSet<RoomType> RoomTypes { get; set; }
        DbSet<AmenityCategory> AmenityCategories { get; set; }
        DbSet<Amenity> Amenities { get; set; }
        DbSet<HotelAmenity> HotelAmenities { get; set; }
        DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }
        DbSet<HotelImage> HotelImages { get; set; }
        DbSet<RoomTypeImage> RoomTypeImages { get; set; }
        DbSet<Province> Provinces { get; set; }
        DbSet<Ward> Wards { get; set; }
        DbSet<Booking> Bookings { get; set; }
        DbSet<BookingItem> BookingItems { get; set; }
        DbSet<HotelCancellationPolicy> HotelCancellationPolicies { get; set; }
        DbSet<HotelDepositPolicy> HotelDepositPolicies { get; set; }
        DbSet<Review> Reviews { get; set; }
        DbSet<Payment> Payments { get; set; }

        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
