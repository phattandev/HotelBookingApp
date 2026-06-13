namespace HotelBookingApp.Application.DTOs.BusinessDto
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string Role { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
