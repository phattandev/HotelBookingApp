using System;
using System.Collections.Generic;
using HotelBookingApp.Application.Common.Interfaces;
using HotelBookingApp.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Infrastructure.Persistence;

public partial class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext()
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Amenity> Amenities { get; set; }

    public virtual DbSet<AmenityCategory> AmenityCategories { get; set; }

    public virtual DbSet<Hotel> Hotels { get; set; }

    public virtual DbSet<HotelAmenity> HotelAmenities { get; set; }

    public virtual DbSet<Province> Provinces { get; set; }

    public virtual DbSet<RoomType> RoomTypes { get; set; }

    public virtual DbSet<RoomTypeAmenity> RoomTypeAmenities { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Ward> Wards { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Amenity>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("amenities_pkey");

            entity.ToTable("amenities");

            entity.HasIndex(e => e.Id, "idx_active_amenities").HasFilter("(is_active = true)");

            entity.HasIndex(e => new { e.Name, e.CategoryId }, "unique_category_amenity").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.IconUrl)
                .HasMaxLength(255)
                .HasColumnName("icon_url");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.HasOne(d => d.Category).WithMany(p => p.Amenities)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_amenity_category");
        });

        modelBuilder.Entity<AmenityCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("amenity_categories_pkey");

            entity.ToTable("amenity_categories");

            entity.HasIndex(e => e.Name, "amenity_categories_name_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValue((short)0)
                .HasColumnName("display_order");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.TargetType)
                .HasMaxLength(20)
                .HasColumnName("target_type");
        });

        modelBuilder.Entity<Hotel>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("hotels_pkey");

            entity.ToTable("hotels");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(300)
                .HasColumnName("name");
            entity.Property(e => e.WardId).HasColumnName("ward_id");

            entity.HasOne(d => d.Ward).WithMany(p => p.Hotels)
                .HasForeignKey(d => d.WardId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_hotel_ward");
        });

        modelBuilder.Entity<HotelAmenity>(entity =>
        {
            entity.HasKey(e => new { e.HotelId, e.AmenityId }).HasName("hotel_amenities_pkey");

            entity.ToTable("hotel_amenities");

            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.AmenityId).HasColumnName("amenity_id");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("added_at");

            entity.HasOne(d => d.Amenity).WithMany(p => p.HotelAmenities)
                .HasForeignKey(d => d.AmenityId)
                .HasConstraintName("fk_ha_amenity");

            entity.HasOne(d => d.Hotel).WithMany(p => p.HotelAmenities)
                .HasForeignKey(d => d.HotelId)
                .HasConstraintName("fk_ha_hotel");
        });

        modelBuilder.Entity<Province>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("provinces_pkey");

            entity.ToTable("provinces");

            entity.HasIndex(e => e.Code, "provinces_code_key").IsUnique();

            entity.HasIndex(e => e.Slug, "provinces_slug_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Slug)
                .HasMaxLength(100)
                .HasColumnName("slug");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");
        });

        modelBuilder.Entity<RoomType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("room_types_pkey");

            entity.ToTable("room_types");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HotelId).HasColumnName("hotel_id");
            entity.Property(e => e.Name)
                .HasMaxLength(200)
                .HasColumnName("name");

            entity.HasOne(d => d.Hotel).WithMany(p => p.RoomTypes)
                .HasForeignKey(d => d.HotelId)
                .HasConstraintName("fk_room_type_hotel");
        });

        modelBuilder.Entity<RoomTypeAmenity>(entity =>
        {
            entity.HasKey(e => new { e.RoomTypeId, e.AmenityId }).HasName("room_type_amenities_pkey");

            entity.ToTable("room_type_amenities");

            entity.Property(e => e.RoomTypeId).HasColumnName("room_type_id");
            entity.Property(e => e.AmenityId).HasColumnName("amenity_id");
            entity.Property(e => e.Quantity)
                .HasDefaultValue((short)1)
                .HasColumnName("quantity");

            entity.HasOne(d => d.Amenity).WithMany(p => p.RoomTypeAmenities)
                .HasForeignKey(d => d.AmenityId)
                .HasConstraintName("fk_rta_amenity");

            entity.HasOne(d => d.RoomType).WithMany(p => p.RoomTypeAmenities)
                .HasForeignKey(d => d.RoomTypeId)
                .HasConstraintName("fk_rta_room_type");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("users_pkey");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "users_email_key").IsUnique();

            entity.HasIndex(e => e.Phone, "users_phone_key").IsUnique();

            entity.Property(e => e.Id)
                .UseIdentityAlwaysColumn()
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(50)
                .HasDefaultValueSql("'customer'::character varying")
                .HasColumnName("role");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("timestamp without time zone")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<Ward>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("wards_pkey");

            entity.ToTable("wards");

            entity.HasIndex(e => e.ProvinceId, "idx_ward_province");

            entity.HasIndex(e => e.Code, "wards_code_key").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code)
                .HasMaxLength(10)
                .HasColumnName("code");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ProvinceId).HasColumnName("province_id");
            entity.Property(e => e.Slug)
                .HasMaxLength(100)
                .HasColumnName("slug");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");

            entity.HasOne(d => d.Province).WithMany(p => p.Wards)
                .HasForeignKey(d => d.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_ward_province");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
