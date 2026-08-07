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
        
        // --- Added for Admin Approval View ---
        public string? BusinessName { get; set; }
        public string? BusinessTaxCode { get; set; }
        public string? BusinessAddress { get; set; }
        public string? RepresentativeName { get; set; }
        public string? RejectionReason { get; set; }
        public int RoomTypeCount { get; set; }
        public int StaffCount { get; set; }
    }
}
