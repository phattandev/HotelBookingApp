using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using HotelBookingApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace HotelBookingApp.Infrastructure.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var section = configuration.GetSection("Cloudinary");
            var account = new Account(
                section["CloudName"],
                section["ApiKey"],
                section["ApiSecret"]
            );
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        public async Task<CloudinaryUploadResult> UploadImageAsync(Stream fileStream, string fileName, string folder)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File ảnh không hợp lệ.");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, fileStream),
                Folder = folder,
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto")
            };

            var result = await _cloudinary.UploadAsync(uploadParams);

            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary upload lỗi: {result.Error.Message}");

            return new CloudinaryUploadResult(result.SecureUrl.ToString(), result.PublicId);
        }

        public async Task<CloudinaryUploadResult> UploadRawFileAsync(Stream fileStream, string fileName, string folder)
        {
            if (fileStream == null || fileStream.Length == 0)
                throw new ArgumentException("File không hợp lệ.");

            var ext = System.IO.Path.GetExtension(fileName).ToLowerInvariant();
            
            if (ext == ".pdf")
            {
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folder
                };
                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                    throw new InvalidOperationException($"Cloudinary upload lỗi: {result.Error.Message}");
                return new CloudinaryUploadResult(result.SecureUrl.ToString(), result.PublicId);
            }
            else
            {
                var uploadParams = new RawUploadParams
                {
                    File = new FileDescription(fileName, fileStream),
                    Folder = folder
                };
                var result = await _cloudinary.UploadAsync(uploadParams);
                if (result.Error != null)
                    throw new InvalidOperationException($"Cloudinary upload lỗi: {result.Error.Message}");
                return new CloudinaryUploadResult(result.SecureUrl.ToString(), result.PublicId);
            }
        }

        public async Task DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            await _cloudinary.DestroyAsync(deleteParams);
        }
    }
}
