using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Infrastructure.Configurations;

namespace Imate.API.Business.Services.ExternalServices
{
    public class AwsS3StorageService : IAwsS3StorageService
    {
        private readonly AwsS3Config _config;
        private readonly IAmazonS3 _s3Client;

        public AwsS3StorageService(IConfiguration configuration)
        {
            _config = new AwsS3Config
            {
                BucketName = configuration["AwsS3Storage:BucketName"] ?? "",
                AccessKey = configuration["AwsS3Storage:AccessKey"] ?? "",
                SecretKey = configuration["AwsS3Storage:SecretKey"] ?? "",
                RegionName = configuration["AwsS3Storage:RegionName"] ?? "ap-southeast-1"
            };

            var region = RegionEndpoint.GetBySystemName(_config.RegionName);
            var s3Config = new AmazonS3Config
            {
                RegionEndpoint = region
            };

            _s3Client = new AmazonS3Client(_config.AccessKey, _config.SecretKey, s3Config);
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File không hợp lệ.");
            }

            string fileExtension = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            string keyName = $"{folderName}/{uniqueFileName}";

            try
            {
                byte[] fileBytes;
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);
                    fileBytes = memoryStream.ToArray();
                }

                using (var uploadStream = new MemoryStream(fileBytes))
                {
                    var putRequest = new PutObjectRequest
                    {
                        BucketName = _config.BucketName,
                        Key = keyName,
                        InputStream = uploadStream,
                        ContentType = file.ContentType
                        // Note: Không sử dụng CannedACL vì bucket không cho phép ACLs
                        // Để file public, cần cấu hình bucket policy thay vì ACL
                    };

                    await _s3Client.PutObjectAsync(putRequest);
                }

                // AWS S3 URL format: https://{bucket}.s3.{region}.amazonaws.com/{filePath}
                return $"https://{_config.BucketName}.s3.{_config.RegionName}.amazonaws.com/{keyName}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể upload file lên AWS S3: {ex.Message}", ex);
            }
        }

        public async Task<string> UploadBytesAsync(byte[] data, string contentType, string folderName, string? fileName = null)
        {
            if (data == null || data.Length == 0)
            {
                throw new ArgumentException("Audio data không hợp lệ.");
            }

            var extension = contentType switch
            {
                "audio/mpeg" => ".mp3",
                "audio/wav" => ".wav",
                _ => ".bin"
            };

            var uniqueFileName = !string.IsNullOrWhiteSpace(fileName)
                ? fileName
                : $"{Guid.NewGuid()}{extension}";

            var keyName = $"{folderName.TrimEnd('/')}/{uniqueFileName}";

            try
            {
                using var uploadStream = new MemoryStream(data);
                var putRequest = new PutObjectRequest
                {
                    BucketName = _config.BucketName,
                    Key = keyName,
                    InputStream = uploadStream,
                    ContentType = contentType
                };

                await _s3Client.PutObjectAsync(putRequest);

                return $"https://{_config.BucketName}.s3.{_config.RegionName}.amazonaws.com/{keyName}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể upload dữ liệu lên AWS S3: {ex.Message}", ex);
            }
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            try
            {
                // Extract key từ URL
                // VD: https://bucket-name.s3.ap-southeast-1.amazonaws.com/Imate-recordings/usercv/abc-123.pdf
                // => keyName = Imate-recordings/usercv/abc-123.pdf

                var uri = new Uri(fileUrl);
                var pathSegments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
                
                // Bỏ qua segment đầu tiên nếu là bucket name
                // Trong AWS S3 URL, path sau domain là key name
                var keyName = string.Join("/", pathSegments);

                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _config.BucketName,
                    Key = keyName
                };

                await _s3Client.DeleteObjectAsync(deleteRequest);
            }
            catch (Exception ex)
            {
                // Log error nhưng không throw để không ảnh hưởng flow update
                Console.WriteLine($"Không thể xóa file từ AWS S3: {ex.Message}");
            }
        }

        public async Task<byte[]> DownloadFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                throw new ArgumentException("File URL không hợp lệ.");
            }

            try
            {
                var uri = new Uri(fileUrl);
                var keyName = string.Join("/", uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries));

                var getRequest = new GetObjectRequest
                {
                    BucketName = _config.BucketName,
                    Key = keyName
                };

                using var response = await _s3Client.GetObjectAsync(getRequest);
                using var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể download file từ AWS S3: {ex.Message}", ex);
            }
        }
    }
}

