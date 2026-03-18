using Amazon.S3;
using Amazon.S3.Transfer;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Infrastructure.Configurations;
using Microsoft.Extensions.Options;

namespace Imate.API.ExternalServices
{
    public class AwsS3StorageService : IAwsS3StorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AwsS3Config _config;

        public AwsS3StorageService(IAmazonS3 s3Client, IOptions<AwsS3Config> config)
        {
            _s3Client = s3Client;
            _config = config.Value;
        }

        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            using var stream = file.OpenReadStream();
            var fileExtension = Path.GetExtension(file.FileName);
            var key = $"{folderName}/{Guid.NewGuid()}{fileExtension}";

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = _config.BucketName,
                ContentType = file.ContentType
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            return $"https://{_config.BucketName}.s3.{_config.RegionName}.amazonaws.com/{key}";
        }

        public async Task<string> UploadBytesAsync(byte[] data, string contentType, string folderName, string? fileName = null)
        {
            using var stream = new MemoryStream(data);
            var fileExtension = !string.IsNullOrEmpty(fileName) ? Path.GetExtension(fileName) : string.Empty;
            var key = $"{folderName}/{Guid.NewGuid()}{fileExtension}";

            var uploadRequest = new TransferUtilityUploadRequest
            {
                InputStream = stream,
                Key = key,
                BucketName = _config.BucketName,
                ContentType = contentType
            };

            var fileTransferUtility = new TransferUtility(_s3Client);
            await fileTransferUtility.UploadAsync(uploadRequest);

            return $"https://{_config.BucketName}.s3.{_config.RegionName}.amazonaws.com/{key}";
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            try
            {
                var uri = new Uri(fileUrl);
                var key = uri.AbsolutePath.TrimStart('/');

                await _s3Client.DeleteObjectAsync(_config.BucketName, key);
            }
            catch
            {
                // Log effectively or handle according to requirements
            }
        }

        public async Task<byte[]> DownloadFileAsync(string fileUrl)
        {
            var uri = new Uri(fileUrl);
            var key = uri.AbsolutePath.TrimStart('/');

            var response = await _s3Client.GetObjectAsync(_config.BucketName, key);
            using var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
