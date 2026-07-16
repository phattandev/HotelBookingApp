namespace HotelBookingApp.Application.DTOs.AmenityDto
{
    public class AmenityCategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string ApplicableTo { get; set; } = "both";
    }
}
