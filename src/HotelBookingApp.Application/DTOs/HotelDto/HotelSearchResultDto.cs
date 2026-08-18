namespace HotelBookingApp.Application.DTOs.HotelDto
{
    /// <summary>
    /// DTO trả về cho mỗi khách sạn trong kết quả tìm kiếm.
    /// </summary>
    public class HotelSearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string ProvinceName { get; set; } = string.Empty;
        public string WardName { get; set; } = string.Empty;
        public int? StarRating { get; set; }
        public string? PrimaryImageUrl { get; set; }   // Ảnh đại diện (isPrimary hoặc ảnh đầu)
        public decimal? MinPrice { get; set; }          // Giá phòng thấp nhất trong KS
        public string? Description { get; set; }

        public List<HotelSearchResultRoomTypeDto> RoomTypes { get; set; } = new();
        public List<HotelSearchResultAmenityDto> Amenities { get; set; } = new();
    }

    public class HotelSearchResultRoomTypeDto
    {
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
    }

    public class HotelSearchResultAmenityDto
    {
        public string Name { get; set; } = null!;
    }
}
