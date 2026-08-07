using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models;

[Table("booking_items")]
public class BookingItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("booking_id")]
    public Guid BookingId { get; set; }

    [Required]
    [Column("room_type_id")]
    public Guid RoomTypeId { get; set; }

    [Required]
    [Column("num_rooms")]
    public int NumRooms { get; set; } = 1;

    [Required]
    [Column("unit_price", TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Column("sub_total", TypeName = "decimal(18,2)")]
    public decimal SubTotal { get; set; }

    [ForeignKey(nameof(BookingId))]
    public virtual Booking Booking { get; set; } = null!;

    [ForeignKey(nameof(RoomTypeId))]
    public virtual RoomType RoomType { get; set; } = null!;
}
