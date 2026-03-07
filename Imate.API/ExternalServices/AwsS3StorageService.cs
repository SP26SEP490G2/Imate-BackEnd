using Microsoft.AspNetCore.Http;
using Imate.API.Business.Interfaces.ExternalServices;

namespace Imate.API.ExternalServices
{
    public class AwsS3StorageService : IAwsS3StorageService
    {
        public async Task<string> UploadFileAsync(IFormFile file, string folderName)
        {
            await Task.Delay(100);
            return "https://dummy-s3-url.com/" + folderName + "/" + file.FileName;
        }

        public async Task<string> UploadBytesAsync(byte[] data, string contentType, string folderName, string? fileName = null)
        {
            await Task.Delay(100);
            return "https://dummy-s3-url.com/" + folderName + "/" + (fileName ?? "dummy.bin");
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            await Task.Delay(100);
        }
    }
}
