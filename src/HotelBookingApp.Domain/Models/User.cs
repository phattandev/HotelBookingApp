using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Domain.Models;

[Table("users")]
[Index(nameof(Email), IsUnique = true, Name = "idx_users_email")]
[Index(nameof(Username), IsUnique = true, Name = "idx_users_username")]
public partial class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("role_id")]
    public Guid RoleId { get; set; }

    [Column("business_id")]
    public Guid? BusinessId { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("username")]
    public string Username { get; set; } = null!;

    [Required]
    [Column("password_hash")]
    public string PasswordHash { get; set; } = null!;

    [Required]
    [EmailAddress]
    [MaxLength(150)]
    [Column("email")]
    public string Email { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = null!;

    [MaxLength(15)]
    [Phone]
    [Column("phone")]
    public string? Phone { get; set; }

    [MaxLength(10)]
    [Column("gender")]
    public string? Gender { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("refresh_token")]
    public string? RefreshToken { get; set; }

    [Column("refresh_token_expiry_time")]
    public DateTime? RefreshTokenExpiryTime { get; set; }

    // --- Navigation Properties ---
    [ForeignKey(nameof(RoleId))]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey(nameof(BusinessId))]
    [InverseProperty(nameof(Business.Employees))] // Mapping 1: Nhân viên thuộc về Doanh nghiệp
    public virtual Business? Business { get; set; }

    [InverseProperty(nameof(Business.Owner))] // Mapping 2: Các Doanh nghiệp mà User này làm chủ
    public virtual ICollection<Business> OwnedBusinesses { get; set; } = new List<Business>();

    [InverseProperty(nameof(HotelStaffAssignment.User))]
    public virtual ICollection<HotelStaffAssignment> StaffAssignments { get; set; } = new List<HotelStaffAssignment>();
}