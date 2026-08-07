using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBookingApp.Application.DTOs.BookingDto
{
    /// <summary>
    /// DTO trả về thông tin đơn đặt phòng (dùng cho cả list và detail).
    /// </summary>
    public class BookingDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly CheckInDate { get; set; }
        public DateOnly CheckOutDate { get; set; }
    // Removed NumRooms
        public decimal TotalPrice { get; set; }
        public string GuestName { get; set; } = null!;
        public string GuestPhone { get; set; } = null!;
        public string GuestEmail { get; set; } = null!;
        public string? SpecialRequests { get; set; }
        public string? CancelReason { get; set; }
        public DateTime CreatedAt { get; set; }

        // Thông tin phòng và khách sạn (join để hiển thị)
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = null!;
        public string HotelAddress { get; set; } = null!;
        public List<BookingItemDto> Items { get; set; } = new();

        // Thông tin thanh toán cọc
        public string PaymentStatus { get; set; } = "Unpaid";
        public decimal DepositAmount { get; set; }
        public DateTime? DepositDeadline { get; set; }
        public DateTime? PaidAt { get; set; }
        public decimal? RefundAmount { get; set; }

        // Đánh giá
        public bool HasReview { get; set; }
    }
}
