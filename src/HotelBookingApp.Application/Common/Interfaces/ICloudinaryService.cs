namespace HotelBookingApp.Application.Common.Interfaces
{
    public record CloudinaryUploadResult(string Url, string PublicId);

    public interface ICloudinaryService
    {
        /// <summary>Upload một file ảnh lên Cloudinary trong folder chỉ định.</summary>
        Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, string folder);

        /// <summary>Xóa ảnh khỏi Cloudinary theo public_id.</summary>
        Task DeleteImageAsync(string publicId);
    }
}
