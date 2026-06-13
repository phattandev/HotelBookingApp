namespace HotelBookingApp.Application.DTOs.UserDto
{
    public class UserProfileDto
    {
        public string Id { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string Role { get; set; } = null!;

        // Thông tin mở rộng nếu tài khoản này làm chủ Doanh nghiệp (Role = partner)
        public BusinessProfileDto? BusinessInfo { get; set; }
    }

    public class BusinessProfileDto
    {
        public string BusinessId { get; set; } = null!;
        public string BusinessName { get; set; } = null!;
        public string TaxCode { get; set; } = null!;
        public string BusinessAddress { get; set; } = null!;
        public string RepresentativeName { get; set; } = null!;
        public string Position { get; set; } = null!;
        public string VerificationStatus { get; set; } = null!;
    }
}
