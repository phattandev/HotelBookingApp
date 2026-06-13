using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;

namespace HotelBookingApp.Domain.Models;

[Table("wards")]
[Index(nameof(Code), IsUnique = true, Name = "idx_wards_code")]
public class Ward
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("province_id")]
    public Guid ProvinceId { get; set; }

    [Required]
    [MaxLength(10)]
    [Column("code")]
    public string Code { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    [Column("slug")]
    public string Slug { get; set; } = null!;

    [Required]
    [MaxLength(30)]
    [Column("type")]
    public string Type { get; set; } = null!;

    [Column("is_active")]
    public bool? IsActive { get; set; } = true;

    [ForeignKey(nameof(ProvinceId))]
    public virtual Province Province { get; set; } = null!;

    [InverseProperty(nameof(Hotel.Ward))]
    public virtual ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}