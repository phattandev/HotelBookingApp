using System;
using System.Collections.Generic;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Infrastructure.Persistence;

public partial class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<Business> Businesses { get; set; }
    public virtual DbSet<HotelStaffAssignment> HotelStaffAssignments { get; set; }
    public virtual DbSet<Hotel> Hotels { get; set; }
    public virtual DbSet<RoomType> RoomTypes { get; set; }
    public virtual DbSet<AmenityCategory> AmenityCategories { get; set; }
    public virtual DbSet<Amenity> Amenities { get; set; }
    public virtual DbSet<HotelAmenity> HotelAmenities { get; set; }
    public virtual DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }
    public virtual DbSet<Province> Provinces { get; set; }
    public virtual DbSet<Ward> Wards { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // CHỈ CÒN LẠI ĐOẠN NÀY ĐỂ NGĂN LỖI XÓA DÂY CHUYỀN

        modelBuilder.Entity<User>()
            .HasOne(d => d.Role)
            .WithMany(p => p.Users)
            .HasForeignKey(d => d.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(d => d.Business)
            .WithMany(p => p.Employees)
            .HasForeignKey(d => d.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Business>()
            .HasOne(d => d.Owner)
            .WithMany(p => p.OwnedBusinesses)
            .HasForeignKey(d => d.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Hotel>()
            .HasOne(d => d.Business)
            .WithMany(p => p.Hotels)
            .HasForeignKey(d => d.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Hotel>()
            .HasOne(d => d.Ward)
            .WithMany(p => p.Hotels)
            .HasForeignKey(d => d.WardId)
            .OnDelete(DeleteBehavior.Restrict);

        // Lưu enum HotelApprovalStatus thành string trong DB (tương thích dữ liệu cũ)
        modelBuilder.Entity<Hotel>()
            .Property(h => h.ApprovalStatus)
            .HasConversion<string>();
    }
}
