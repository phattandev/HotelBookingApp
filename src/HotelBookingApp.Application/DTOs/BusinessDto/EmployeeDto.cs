namespace HotelBookingApp.Application.DTOs.BusinessDto
{
    public class EmployeeDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string Username { get; set; } = null!;
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string Role { get; set; } = null!;     // Role hệ thống (staff/manager/receptionist)
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }

        // Thông tin phân công (null nếu chưa phân công)
        public Guid? AssignedHotelId { get; set; }
        public string? AssignedHotelName { get; set; }
        public string? RoleInHotel { get; set; }       // manager / receptionist
    }
}
