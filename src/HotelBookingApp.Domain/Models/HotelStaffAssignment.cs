using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelBookingApp.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Domain.Models
{
    [Table("hotel_staff_assignments")]
    [Index(nameof(UserId), Name = "idx_staff_assignments_user_id")]
    public class HotelStaffAssignment
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("user_id")]
        public Guid UserId { get; set; }

        [Required]
        [Column("hotel_id")]
        public Guid HotelId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("role_in_hotel")]
        public string RoleInHotel { get; set; } = "staff"; // "manager" hoặc "staff"

        [Column("assigned_at")]
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [ForeignKey(nameof(HotelId))]
        public virtual Hotel Hotel { get; set; } = null!;
    }
}
