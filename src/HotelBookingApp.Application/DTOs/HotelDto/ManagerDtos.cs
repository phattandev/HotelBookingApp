namespace HotelBookingApp.Application.DTOs.HotelDto
{
    public class HotelImageDto
    {
        public Guid Id { get; set; }
        public string Url { get; set; } = null!;
        public string PublicId { get; set; } = null!;
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class AmenityDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string ApplicableTo { get; set; } = null!; // hotel | room | both
    }

    public class RoomTypeDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal BasePrice { get; set; }
        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        public int TotalRooms { get; set; }
        public int BookedRooms { get; set; }
        public int AvailableRooms { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<HotelImageDto> Images { get; set; } = new();
        public List<AmenityDto> Amenities { get; set; } = new();
    }

    public class ManagedHotelDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ProvinceName { get; set; } = string.Empty;
        public Guid? ProvinceId { get; set; }
        public string WardName { get; set; } = string.Empty;
        public Guid? WardId { get; set; }
        public string AddressLine { get; set; } = null!;
        public string? Description { get; set; }
        public int? StarRating { get; set; }
        public string ApprovalStatus { get; set; } = null!;
        public bool IsActive { get; set; }
        public List<HotelImageDto> Images { get; set; } = new();
        public List<AmenityDto> Amenities { get; set; } = new();
        public List<RoomTypeDto> RoomTypes { get; set; } = new();
    }
}
