using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HotelBookingApp.Domain.Models;

[Table("amenity_categories")]
public class AmenityCategory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    [Column("name")]
    public string Name { get; set; } = null!;

    /// <summary>"hotel" | "room" | "both" — Admin phân loại tiện nghi này dành cho loại nào</summary>
    [MaxLength(10)]
    [Column("applicable_to")]
    public string ApplicableTo { get; set; } = "both";

    [InverseProperty(nameof(Amenity.Category))]
    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();
}
