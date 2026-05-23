using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelBookingApp.Application.DTOs.WardDto
{
    public class WardDto
    {
        public int Id { get; set; }
        public short ProvinceId { get; set; }
        public string ProvinceName { get; set; } = null!; // Hiển thị tên Tỉnh/Thành
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool? IsActive { get; set; }
    }
}
