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
    public virtual DbSet<BusinessStaff> BusinessStaff { get; set; }
    public virtual DbSet<HotelStaffAssignment> HotelStaffAssignments { get; set; }
    public virtual DbSet<Hotel> Hotels { get; set; }
    public virtual DbSet<RoomType> RoomTypes { get; set; }
    public virtual DbSet<AmenityCategory> AmenityCategories { get; set; }
    public virtual DbSet<Amenity> Amenities { get; set; }
    public virtual DbSet<HotelAmenity> HotelAmenities { get; set; }
    public virtual DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }
    public virtual DbSet<HotelImage> HotelImages { get; set; }
    public virtual DbSet<RoomTypeImage> RoomTypeImages { get; set; }
    public virtual DbSet<Province> Provinces { get; set; }
    public virtual DbSet<Ward> Wards { get; set; }
    public virtual DbSet<Booking> Bookings { get; set; }
    public virtual DbSet<HotelCancellationPolicy> HotelCancellationPolicies { get; set; }
    public virtual DbSet<HotelDepositPolicy> HotelDepositPolicies { get; set; }
    public virtual DbSet<Review> Reviews { get; set; }
    public virtual DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Ngăn lỗi Cascade Delete dây chuyền ──────────────────────────────

        modelBuilder.Entity<User>()
            .HasOne(d => d.Role)
            .WithMany(p => p.Users)
            .HasForeignKey(d => d.RoleId)
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

        // ── Unique Constraints ───────────────────────────────────────────────

        // Mã số thuế doanh nghiệp phải là duy nhất trong hệ thống
        modelBuilder.Entity<Business>()
            .HasIndex(b => b.TaxCode)
            .IsUnique()
            .HasDatabaseName("idx_businesses_tax_code");

        // ── Check Constraints (nghiệp vụ) ────────────────────────────────────

        // Giá phòng phải > 0
        modelBuilder.Entity<RoomType>()
            .ToTable(t => t.HasCheckConstraint("chk_roomtype_baseprice", "\"base_price\" > 0"));

        // Tổng số phòng phải >= 1
        modelBuilder.Entity<RoomType>()
            .ToTable(t => t.HasCheckConstraint("chk_roomtype_totalrooms", "\"total_rooms\" >= 1"));

        // Tỉ lệ phạt phải nằm trong [0, 100]
        modelBuilder.Entity<HotelCancellationPolicy>()
            .ToTable(t => t.HasCheckConstraint(
                "chk_policy_penalty_percentage",
                "\"penalty_percentage\" >= 0 AND \"penalty_percentage\" <= 100"));

        // Mốc giờ hủy phải > 0
        modelBuilder.Entity<HotelCancellationPolicy>()
            .ToTable(t => t.HasCheckConstraint(
                "chk_policy_hours_before",
                "\"hours_before_check_in\" > 0"));

        // ── Lưu Enum thành string trong DB ───────────────────────────────────

        // Tương thích dữ liệu cũ đang lưu dạng string
        modelBuilder.Entity<Hotel>()
            .Property(h => h.ApprovalStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Business>()
            .Property(b => b.VerificationStatus)
            .HasConversion<string>();

        modelBuilder.Entity<Booking>()
            .Property(b => b.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Booking>()
            .Property(b => b.PaymentStatus)
            .HasConversion<string>();

        // ── Quan hệ Booking ──────────────────────────────────────────────────

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.RoomType)
            .WithMany()
            .HasForeignKey(b => b.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Customer)
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK: Booking → CancellationPolicy (nullable snapshot, SetNull khi xóa policy)
        modelBuilder.Entity<Booking>()
            .HasOne<HotelCancellationPolicy>()
            .WithMany()
            .HasForeignKey(b => b.CancellationPolicyId)
            .OnDelete(DeleteBehavior.SetNull);

        // ── Quan hệ CancellationPolicy ───────────────────────────────────────

        // 1 hotel = đúng 1 policy (Unique Index đã khai báo trên model với [Index])
        modelBuilder.Entity<Hotel>()
            .HasOne(h => h.CancellationPolicy)
            .WithOne(p => p.Hotel)
            .HasForeignKey<HotelCancellationPolicy>(p => p.HotelId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa hotel → xóa luôn policy

        // ── Quan hệ DepositPolicy ───────────────────────────────────────

        // 1 hotel = đúng 1 deposit policy (Unique Index khai báo trên model)
        modelBuilder.Entity<HotelDepositPolicy>()
            .HasOne(p => p.Hotel)
            .WithOne(h => h.DepositPolicy)
            .HasForeignKey<HotelDepositPolicy>(p => p.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── Quan hệ Review ────────────────────────────────────────────────────

        // Mỗi Booking chỉ có đúng 1 Review (1-1)
        modelBuilder.Entity<Review>()
            .HasIndex(r => r.BookingId)
            .IsUnique()
            .HasDatabaseName("idx_reviews_booking_id");

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Booking)
            .WithMany()
            .HasForeignKey(r => r.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Hotel)
            .WithMany()
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Review>()
            .HasOne(r => r.Customer)
            .WithMany()
            .HasForeignKey(r => r.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
