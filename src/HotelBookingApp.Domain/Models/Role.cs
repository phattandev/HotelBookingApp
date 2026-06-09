using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotelBookingApp.Infrastructure;

namespace HotelBookingApp.Domain.Models
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("name")]
        public string Name { get; set; } = null!;

        // Navigation
        [InverseProperty(nameof(Models.Role))]
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
