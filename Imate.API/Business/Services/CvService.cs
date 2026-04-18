using Imate.AI.Module.Core.Interfaces;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;

namespace Imate.API.Business.Services
{
    public class CvService : ICvService
    {
        private readonly IUserCvRepository _cvRepository;
        private readonly IAwsS3StorageService _s3Storage;
        private readonly ICvAnalysisOrchestrator _cvAnalysisOrchestrator;

        // File validation constants
        private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5MB
        private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
        private static readonly string[] AllowedContentTypes =
        {
            "application/pdf",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        public CvService(
            IUserCvRepository cvRepository,
            IAwsS3StorageService s3Storage,
            ICvAnalysisOrchestrator cvAnalysisOrchestrator)
        {
            _cvRepository = cvRepository;
            _s3Storage = s3Storage;
            _cvAnalysisOrchestrator = cvAnalysisOrchestrator;
        }

        /// <summary>
        /// Upload CV: validate → S3 upload → save entity to DB
        /// </summary>
        public async Task<UserCv> UploadCvAsync(int accountId, IFormFile file, string fileName)
        {
            // 1. Validate file type và size
            ValidateFile(file);
            await _cvAnalysisOrchestrator.ValidateCvIsItAsync(file);

            // 3. Upload lên S3
            var fileUrl = await _s3Storage.UploadFileAsync(file, "cv");

            // 4. Lưu vào DB
            var displayName = !string.IsNullOrWhiteSpace(fileName) ? fileName : file.FileName;
            var userCv = new UserCv
            {
                AccountId = accountId,
                FileName = displayName,
                FileUrl = fileUrl,
                UploadDate = DateTimeOffset.UtcNow,
                ScannedData = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            await _cvRepository.AddAsync(userCv);
            return userCv;
        }

        /// <summary>
        /// Get list of CVs for a given account
        /// </summary>
        public async Task<List<UserCv>> GetListCvAsync(int accountId)
        {
            var cvs = await _cvRepository.GetByAccountIdAsync(accountId);
            return cvs.ToList();
        }

        /// <summary>
        /// Delete a CV: verify ownership → delete from S3 → delete from DB
        /// </summary>
        public async Task DeleteCvAsync(int accountId, int cvId)
        {
            var cv = await _cvRepository.GetByIdAsync(cvId);

            if (cv == null)
                throw new Exception("CV không tồn tại.");

            if (cv.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền xóa CV này.");

            // Delete file from S3
            if (!string.IsNullOrEmpty(cv.FileUrl))
            {
                await _s3Storage.DeleteFileAsync(cv.FileUrl);
            }

            // Delete entity from DB
            await _cvRepository.DeleteAsync(cv);
        }

        /// <summary>
        /// Validate file type and size
        /// </summary>
        private void ValidateFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Vui lòng chọn file CV.");

            if (file.Length > MaxFileSizeBytes)
                throw new ArgumentException("Tệp phải là PDF hoặc DOCX và dưới 5MB.");

            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
                throw new ArgumentException("Tệp phải là PDF hoặc DOCX và dưới 5MB.");

            if (!AllowedContentTypes.Contains(file.ContentType))
                throw new ArgumentException("Tệp phải là PDF hoặc DOCX và dưới 5MB.");
        }
    }
}
