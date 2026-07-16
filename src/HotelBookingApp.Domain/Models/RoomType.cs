using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models;

[Table("room_types")]
public class RoomType
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("hotel_id")]
    public Guid HotelId { get; set; }

    [Required]
    [MaxLength(150)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Required]
    [Column("base_price", TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Required]
    [Column("max_adults")]
    public int MaxAdults { get; set; }

    [Required]
    [Column("max_children")]
    public int MaxChildren { get; set; }

    [Required]
    [Column("total_rooms")]
    public int TotalRooms { get; set; }

    [Column("description", TypeName = "text")]
    public string Description { get; set; } = string.Empty;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(HotelId))]
    public virtual Hotel Hotel { get; set; } = null!;

    [InverseProperty(nameof(RoomTypeAmenity.RoomType))]
    public virtual ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();

    [InverseProperty(nameof(RoomTypeImage.RoomType))]
    public virtual ICollection<RoomTypeImage> Images { get; set; } = new List<RoomTypeImage>();
}
