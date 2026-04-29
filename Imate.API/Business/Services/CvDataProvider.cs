using System.Text;
using Imate.AI.Module.Core.Interfaces;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.Business.Services
{
    /// <summary>
    /// Bridge giữa Imate.API và AI Module
    /// Cung cấp CV text data cho AI Module mà không cần AI Module phụ thuộc vào Imate.API
    /// </summary>
    public class CvDataProvider : ICvDataProvider
    {
        private readonly IUserCvRepository _cvRepository;
        private readonly IAwsS3StorageService _s3Storage;
        private readonly ILogger<CvDataProvider> _logger;
        private readonly ImateDbContext _context;
        private readonly ISystemConfigService _systemConfigService;

        public CvDataProvider(
            IUserCvRepository cvRepository,
            IAwsS3StorageService s3Storage,
            ILogger<CvDataProvider> logger,
            ImateDbContext context,
            ISystemConfigService systemConfigService)
        {
            _cvRepository = cvRepository;
            _s3Storage = s3Storage;
            _logger = logger;
            _context = context;
            _systemConfigService = systemConfigService;
        }

        public async Task<CvAnalysisEligibility> CheckCvAnalysisEligibilityAsync(int accountId)
        {
            // Lấy subscription đang active của user (include Package để lấy Rank)
            var activeSub = await _context.UserSubscriptions
                .Include(s => s.Package)
                .Where(s => s.CandidateId == accountId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            // 1. Không có subscription hoặc package rank = 0 → không được dùng
            if (activeSub == null || activeSub.Package.Rank == 0)
            {
                return new CvAnalysisEligibility
                {
                    IsEligible = false,
                    Message = "Tính năng Phân tích CV yêu cầu gói trả phí. Vui lòng nâng cấp gói để sử dụng dịch vụ này."
                };
            }

            // 2. Lấy số token cần thiết từ SystemConfig
            int tokenCost = await _systemConfigService.GetAnalyseCvCostPointAsync();

            // 3. Kiểm tra số token còn lại (InitialMockLimit - MockInterviewUsed)
            int remaining = activeSub.InitialMockLimit - activeSub.MockInterviewUsed;
            if (remaining < tokenCost)
            {
                return new CvAnalysisEligibility
                {
                    IsEligible = false,
                    Message = $"Bạn không đủ AI Credit để phân tích CV. " +
                              $"Dịch vụ này cần {tokenCost} AI Credit, bạn còn {Math.Max(0, remaining)} AI Credit trong gói {activeSub.Package.Name}."
                };
            }

            _logger.LogInformation(
                "[CvDataProvider] Eligibility OK for accountId={AccountId}: remaining={Remaining}, tokenCost={Cost}",
                accountId, remaining, tokenCost);

            return new CvAnalysisEligibility { IsEligible = true };
        }

        public async Task<string> GetCvTextAsync(int accountId, int cvId)
        {
            _logger.LogInformation("=== [CvDataProvider] GetCvTextAsync START === accountId={AccountId}, cvId={CvId}", accountId, cvId);

            var cv = await _cvRepository.GetByIdAsync(cvId);

            if (cv == null)
                throw new ArgumentException($"Không tìm thấy CV với ID {cvId}");

            if (cv.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập CV này.");

            _logger.LogInformation("[CvDataProvider] CV found: FileName={FileName}, FileUrl={FileUrl}, ScannedData length={Length}",
                cv.FileName, cv.FileUrl, cv.ScannedData?.Length ?? 0);

            // Sử dụng ScannedData nếu đã có
            if (!string.IsNullOrWhiteSpace(cv.ScannedData))
            {
                _logger.LogInformation("[CvDataProvider] Using cached ScannedData ({Length} chars)", cv.ScannedData.Length);
                _logger.LogInformation("[CvDataProvider] ScannedData preview: {Preview}", cv.ScannedData.Substring(0, Math.Min(500, cv.ScannedData.Length)));
                return cv.ScannedData;
            }

            // Auto-extract: Download PDF từ S3 và trích xuất text
            if (string.IsNullOrWhiteSpace(cv.FileUrl))
                throw new InvalidOperationException("CV không có file URL. Vui lòng upload lại CV.");

            _logger.LogInformation("[CvDataProvider] ScannedData is empty. Downloading PDF from S3: {FileUrl}", cv.FileUrl);

            var extractedText = await DownloadAndExtractTextAsync(cv.FileUrl);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                _logger.LogError("[CvDataProvider] PDF text extraction returned EMPTY!");
                throw new InvalidOperationException("Không thể trích xuất nội dung từ file CV. File có thể trống hoặc bị hỏng.");
            }

            _logger.LogInformation("[CvDataProvider] Extracted {Length} chars from file", extractedText.Length);
            _logger.LogInformation("[CvDataProvider] Extracted text preview:\n{Preview}", extractedText.Substring(0, Math.Min(500, extractedText.Length)));

            // DEBUG: Ghi text ra file để verify Vietnamese encoding
            var debugPath = Path.Combine(Path.GetTempPath(), "cv_extracted_debug.txt");
            await File.WriteAllTextAsync(debugPath, extractedText, Encoding.UTF8);
            _logger.LogInformation("[CvDataProvider] DEBUG: Wrote extracted text to {Path}", debugPath);

            // Lưu ScannedData vào DB cho lần sau
            cv.ScannedData = extractedText;
            cv.UpdatedAt = DateTime.UtcNow;
            await _cvRepository.UpdateAsync(cv);
            _logger.LogInformation("[CvDataProvider] Saved ScannedData to DB for cvId={CvId}", cvId);

            return extractedText;
        }

        public async Task<string?> GetCachedAnalysisAsync(int accountId, int cvId)
        {
            var cv = await _cvRepository.GetByIdAsync(cvId);

            if (cv == null || cv.AccountId != accountId)
                return null;

            return cv.AnalysisData;
        }

        public async Task SaveAnalysisResultAsync(int accountId, int cvId, string analysisJson)
        {
            var cv = await _cvRepository.GetByIdAsync(cvId);

            if (cv == null)
                throw new ArgumentException($"Không tìm thấy CV với ID {cvId}");

            if (cv.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật CV này.");

            cv.AnalysisData = analysisJson;
            cv.UpdatedAt = DateTime.UtcNow;
            await _cvRepository.UpdateAsync(cv);
        }

        public async Task ClearScannedDataAsync(int accountId, int cvId)
        {
            var cv = await _cvRepository.GetByIdAsync(cvId);

            if (cv == null || cv.AccountId != accountId)
                return;

            cv.ScannedData = string.Empty;
            cv.UpdatedAt = DateTime.UtcNow;
            await _cvRepository.UpdateAsync(cv);
            _logger.LogInformation("[CvDataProvider] Cleared ScannedData for cvId={CvId}", cvId);
        }

        /// <summary>
        /// Download file từ S3 và trích xuất text từ PDF/DOCX
        /// </summary>
        private async Task<string> DownloadAndExtractTextAsync(string fileUrl)
        {
            _logger.LogInformation("[CvDataProvider] Downloading file from S3...");
            var fileBytes = await _s3Storage.DownloadFileAsync(fileUrl);
            _logger.LogInformation("[CvDataProvider] Downloaded {Size} bytes from S3", fileBytes.Length);

            var extension = Path.GetExtension(new Uri(fileUrl).AbsolutePath)?.ToLowerInvariant();
            _logger.LogInformation("[CvDataProvider] File extension: {Extension}", extension);

            return extension switch
            {
                ".pdf" => ExtractTextFromPdf(fileBytes),
                ".docx" => ExtractTextFromDocx(fileBytes),
                ".doc" => ExtractTextFromDocx(fileBytes), // Thử đọc như DOCX, có thể fail với .doc cũ
                _ => throw new InvalidOperationException($"Không hỗ trợ định dạng file: {extension}")
            };
        }

        /// <summary>
        /// Trích xuất text từ PDF sử dụng PdfPig
        /// </summary>
        private string ExtractTextFromPdf(byte[] pdfBytes)
        {
            var sb = new StringBuilder();

            _logger.LogInformation("[CvDataProvider] Opening PDF with PdfPig...");
            using var document = PdfDocument.Open(pdfBytes);
            _logger.LogInformation("[CvDataProvider] PDF has {PageCount} pages", document.NumberOfPages);

            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                _logger.LogInformation("[CvDataProvider] Page {PageNo}: {Length} chars", page.Number, pageText?.Length ?? 0);
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    sb.AppendLine(pageText);
                }
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Trích xuất text từ DOCX sử dụng OpenXml
        /// </summary>
        private string ExtractTextFromDocx(byte[] docxBytes)
        {
            var sb = new StringBuilder();

            _logger.LogInformation("[CvDataProvider] Opening DOCX with OpenXml...");
            using var stream = new MemoryStream(docxBytes);
            using var wordDoc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(stream, false);

            var body = wordDoc.MainDocumentPart?.Document?.Body;
            if (body == null)
            {
                _logger.LogWarning("[CvDataProvider] DOCX body is null!");
                return string.Empty;
            }

            foreach (var paragraph in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var text = paragraph.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    sb.AppendLine(text);
                }
            }

            var result = sb.ToString().Trim();
            _logger.LogInformation("[CvDataProvider] DOCX extracted {Length} chars", result.Length);
            return result;
        }

        public async Task ConsumeCvAnalysisCostAsync(int accountId)
        {
            // Lấy subscription active
            var activeSub = await _context.UserSubscriptions
                .Include(s => s.Package)
                .Where(s => s.CandidateId == accountId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeSub == null)
                throw new Exception("Không tìm thấy gói subscription.");

            // Lấy cost từ system config
            int tokenCost = await _systemConfigService.GetAnalyseCvCostPointAsync();

            // Trừ token
            activeSub.MockInterviewUsed += tokenCost;

            _logger.LogInformation(
                "[CvDataProvider] Consumed {Cost} tokens for accountId={AccountId}. New used={Used}",
                tokenCost, accountId, activeSub.MockInterviewUsed);

            await _context.SaveChangesAsync();
        }
    }
}