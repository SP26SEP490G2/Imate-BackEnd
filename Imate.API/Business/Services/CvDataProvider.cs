using Imate.AI.Module.Interfaces;
using Imate.API.DataAccess.Interfaces.UserManagement;

namespace Imate.API.Business.Services
{
    /// <summary>
    /// Bridge giữa Imate.API và AI Module
    /// Cung cấp CV text data cho AI Module mà không cần AI Module phụ thuộc vào Imate.API
    /// </summary>
    public class CvDataProvider : ICvDataProvider
    {
        private readonly IUserCvRepository _cvRepository;

        public CvDataProvider(IUserCvRepository cvRepository)
        {
            _cvRepository = cvRepository;
        }

        public async Task<string> GetCvTextAsync(int accountId, int cvId)
        {
            var cv = await _cvRepository.GetByIdAsync(cvId);

            if (cv == null)
                throw new ArgumentException($"Không tìm thấy CV với ID {cvId}");

            if (cv.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập CV này.");

            // Sử dụng ScannedData nếu có
            if (!string.IsNullOrWhiteSpace(cv.ScannedData))
                return cv.ScannedData;

            // TODO: Download từ S3 và extract text nếu chưa có ScannedData
            throw new InvalidOperationException("CV chưa được trích xuất nội dung. Vui lòng thử với CvText trực tiếp.");
        }
    }
}
