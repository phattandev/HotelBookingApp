namespace HotelBookingApp.Application.DTOs.BusinessDto
{
    /// <summary>
    /// DTO trả về thông tin một bản ghi phân công nhân viên vào khách sạn.
    /// </summary>
    public class StaffAssignmentDto
    {
        public Guid AssignmentId { get; set; }
        public Guid EmployeeId { get; set; }
        public string EmployeeFullName { get; set; } = null!;
        public string EmployeeEmail { get; set; } = null!;
        public Guid HotelId { get; set; }
        public string HotelName { get; set; } = null!;
        public string RoleInHotel { get; set; } = null!;  // "manager" | "receptionist"
        public DateTime AssignedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
