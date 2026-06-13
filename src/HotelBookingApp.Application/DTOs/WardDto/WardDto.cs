namespace HotelBookingApp.Application.DTOs.WardDto
{
    public class WardDto
    {
        public Guid Id { get; set; }
        public Guid ProvinceId { get; set; }
        public string ProvinceName { get; set; } = null!; // Hiển thị tên Tỉnh/Thành
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool? IsActive { get; set; }
    }
}
