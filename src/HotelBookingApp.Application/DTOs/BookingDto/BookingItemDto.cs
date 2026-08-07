using System;

namespace HotelBookingApp.Application.DTOs.BookingDto
{
    public class BookingItemDto
    {
        public Guid Id { get; set; }
        public Guid RoomTypeId { get; set; }
        public string RoomTypeName { get; set; } = null!;
        public int NumRooms { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SubTotal { get; set; }
        public string? RoomImageUrl { get; set; }
    }
}
