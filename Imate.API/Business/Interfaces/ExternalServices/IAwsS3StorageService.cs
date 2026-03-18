namespace Imate.API.Business.Interfaces.ExternalServices
{
    public interface IAwsS3StorageService
    {
        Task<string> UploadFileAsync(IFormFile file, string folderName);
        Task<string> UploadBytesAsync(byte[] data, string contentType, string folderName, string? fileName = null);
        Task DeleteFileAsync(string fileUrl);
        Task<byte[]> DownloadFileAsync(string fileUrl);
    }
}

