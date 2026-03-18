using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Imate.AI.Module.Services
{
    /// <summary>
    /// CV Analysis Service - orchestrates Gemini AI analysis
    /// Dependency: ICvDataProvider từ host project cung cấp dữ liệu CV
    /// </summary>
    public class CvAnalysisService : ICvAnalysisService
    {
        private readonly IGeminiService _geminiService;
        private readonly ICvDataProvider? _cvDataProvider;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CvAnalysisService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CvAnalysisService(
            IGeminiService geminiService,
            IWebHostEnvironment env,
            ILogger<CvAnalysisService> logger,
            ICvDataProvider? cvDataProvider = null)
        {
            _geminiService = geminiService;
            _env = env;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
        }

        public async Task<CvAnalysisResponse> AnalyseCvAsync(int accountId, AnalyseCvRequest request)
        {
            // 1. Check cache trước (chỉ khi có cvId và không force reanalyze)
            if (request.CvId.HasValue && _cvDataProvider != null && !request.ForceReanalyze)
            {
                var cached = await _cvDataProvider.GetCachedAnalysisAsync(accountId, request.CvId.Value);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    _logger.LogInformation("Returning cached CV analysis for account {AccountId}, cvId {CvId}", accountId, request.CvId.Value);
                    return ParseGeminiResponse(cached);
                }
            }

            // 2. Nếu force reanalyze, xóa cả ScannedData để re-extract từ file gốc
            if (request.ForceReanalyze && request.CvId.HasValue && _cvDataProvider != null)
            {
                await _cvDataProvider.ClearScannedDataAsync(accountId, request.CvId.Value);
            }

            // 3. Lấy CV text
            string cvText = await GetCvTextAsync(accountId, request);

            if (string.IsNullOrWhiteSpace(cvText))
            {
                throw new ArgumentException("Không có nội dung CV để phân tích. Vui lòng cung cấp CvId hoặc CvText.");
            }

            // 3. Đọc system prompt từ file
            var systemPrompt = await LoadSystemPromptAsync();

            // 4. Gọi Gemini AI
            _logger.LogInformation("=== [CvAnalysis] CV Text to analyze ({Length} chars) ===", cvText.Length);
            _logger.LogInformation("[CvAnalysis] CV Text preview:\n{Preview}", cvText.Substring(0, Math.Min(500, cvText.Length)));
            var userPrompt = $"Hãy phân tích CV sau đây và trả về kết quả dưới dạng JSON:\n\n{cvText}";
            var rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);

            var result = ParseGeminiResponse(rawResponse);
            _logger.LogInformation("CV analysis completed. Score: {Score}, Candidate: {Name}", result.Score, result.CandidateName);

            // 5. Lưu cache vào DB (chỉ khi có cvId)
            if (request.CvId.HasValue && _cvDataProvider != null)
            {
                try
                {
                    await _cvDataProvider.SaveAnalysisResultAsync(accountId, request.CvId.Value, rawResponse);
                    _logger.LogInformation("Cached CV analysis result for cvId {CvId}", request.CvId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache CV analysis result for cvId {CvId}", request.CvId.Value);
                }
            }

            return result;
        }

        /// <summary>
        /// Đọc system prompt từ file SystemMessages/analyse-cv-system.txt
        /// Tìm file dựa trên vị trí assembly của AI module
        /// </summary>
        private async Task<string> LoadSystemPromptAsync()
        {
            // Tìm thư mục gốc của AI module dựa trên assembly location
            var assemblyDir = Path.GetDirectoryName(typeof(CvAnalysisService).Assembly.Location)!;
            var filePath = Path.Combine(assemblyDir, "SystemMessages", "analyse-cv-system.txt");

            // Fallback: tìm trong ContentRootPath (cho development)
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(_env.ContentRootPath, "..", "Imate.AI.Module", "SystemMessages", "analyse-cv-system.txt");
            }

            // Fallback 2: tìm trực tiếp trong ContentRootPath
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(_env.ContentRootPath, "SystemMessages", "analyse-cv-system.txt");
            }

            if (!File.Exists(filePath))
            {
                _logger.LogError("System prompt file not found. Searched paths include assembly dir and ContentRootPath");
                throw new FileNotFoundException($"System prompt file not found. Hãy tạo file SystemMessages/analyse-cv-system.txt trong Imate.AI.Module");
            }

            var content = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("System prompt file is empty: " + filePath);
            }

            _logger.LogInformation("Loaded system prompt from file: {Path}", filePath);
            return content;
        }

        private async Task<string> GetCvTextAsync(int accountId, AnalyseCvRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.CvText))
            {
                return request.CvText;
            }

            if (request.CvId.HasValue)
            {
                if (_cvDataProvider == null)
                    throw new InvalidOperationException("ICvDataProvider chưa được đăng ký. Không thể truy vấn CV từ database.");

                return await _cvDataProvider.GetCvTextAsync(accountId, request.CvId.Value);
            }

            throw new ArgumentException("Vui lòng cung cấp CvId hoặc CvText.");
        }

        private CvAnalysisResponse ParseGeminiResponse(string responseText)
        {
            var cleaned = Regex.Replace(responseText.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "");
            cleaned = cleaned.Trim();

            try
            {
                var result = JsonSerializer.Deserialize<CvAnalysisResponse>(cleaned, JsonOptions);
                if (result == null)
                    throw new Exception("Parsed result is null");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini response: {Response}", cleaned.Substring(0, Math.Min(500, cleaned.Length)));
                throw new Exception("Không thể phân tích phản hồi từ AI. Vui lòng thử lại.", ex);
            }
        }
    }
}
