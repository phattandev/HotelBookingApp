namespace HotelBookingApp.Application.DTOs.HotelDto
{
    public class HotelDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string AddressLine { get; set; } = null!;
        public string TaxCode { get; set; } = null!;
        public string ApprovalStatus { get; set; } = null!;
        public bool IsActive { get; set; }
    }
}
