namespace HotelBookingApp.Application.DTOs.HotelDto
{
    /// <summary>
    /// DTO trả về một gợi ý địa điểm (tỉnh/thành phố hoặc quận/huyện/phường/xã)
    /// có ít nhất 1 khách sạn đang hoạt động trong hệ thống.
    /// </summary>
    public class LocationSuggestionDto
    {
        /// <summary>Tên hiển thị đầy đủ (ví dụ: "Đà Nẵng" hoặc "Quận Hải Châu, Đà Nẵng")</summary>
        public string DisplayName { get; set; } = null!;

        /// <summary>Từ khóa sẽ điền vào ô tìm kiếm khi user chọn</summary>
        public string Value { get; set; } = null!;

        /// <summary>"province" hoặc "ward"</summary>
        public string Type { get; set; } = null!;

        /// <summary>Số lượng khách sạn tại địa điểm này</summary>
        public int HotelCount { get; set; }
    }
}
