namespace HotelBookingApp.Application.DTOs.ProvinceDto
{
    public class ProvinceDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool? IsActive { get; set; }
    }
}
