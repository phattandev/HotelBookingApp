namespace HotelBookingApp.Application.DTOs.AmenityDto
{
    public class AmenityDto
    {
        public Guid Id { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool? IsActive { get; set; }
    }
}
