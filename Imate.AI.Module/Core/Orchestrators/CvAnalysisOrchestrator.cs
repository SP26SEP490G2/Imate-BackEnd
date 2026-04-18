using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;


namespace Imate.AI.Module.Core.Orchestrators
{
    /// <summary>
    /// Orchestrator phân tích CV (Tầng 2 - Orchestrators)
    /// Điều phối workflow:
    ///   - AnalyseCvAsync: cache check → data access → Agent → save cache
    ///   - ValidateCvIsItAsync: extract text → AI check → reject nếu không phải IT
    /// </summary>
    public class CvAnalysisOrchestrator : ICvAnalysisOrchestrator
    {
        private readonly ICvAnalysisAgent _cvAnalysisAgent;
        private readonly IGeminiService _geminiService;
        private readonly ICvDataProvider? _cvDataProvider;
        private readonly ILogger<CvAnalysisOrchestrator> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CvAnalysisOrchestrator(
            ICvAnalysisAgent cvAnalysisAgent,
            IGeminiService geminiService,
            ILogger<CvAnalysisOrchestrator> logger,
            ICvDataProvider? cvDataProvider = null)
        {
            _cvAnalysisAgent = cvAnalysisAgent;
            _geminiService = geminiService;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
        }

        // =====================================================================
        // ANALYSE CV
        // =====================================================================

        public async Task<CvAnalysisResponse> AnalyseCvAsync(int accountId, AnalyseCvRequest request)
        {
            // 1. Check cache trước (chỉ khi có cvId và không force reanalyze)
            if (request.CvId.HasValue && _cvDataProvider != null && !request.ForceReanalyze)
            {
                var cached = await _cvDataProvider.GetCachedAnalysisAsync(accountId, request.CvId.Value);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    try
                    {
                        _logger.LogInformation("Returning cached CV analysis for account {AccountId}, cvId {CvId}", accountId, request.CvId.Value);
                        var parsed = JsonSerializer.Deserialize<CvAnalysisResponse>(cached, JsonOptions);
                        if (parsed != null) return parsed;
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Cached CV analysis data is invalid JSON for cvId {CvId}, will re-analyze.", request.CvId.Value);
                        // Cache bị hỏng → xóa cache cũ và phân tích lại
                        await _cvDataProvider.SaveAnalysisResultAsync(accountId, request.CvId.Value, null);
                    }
                }
            }

            // 2. Nếu force reanalyze, xóa ScannedData để re-extract từ file gốc
            if (request.ForceReanalyze && request.CvId.HasValue && _cvDataProvider != null)
                await _cvDataProvider.ClearScannedDataAsync(accountId, request.CvId.Value);

            // 3. Lấy CV text
            string cvText = await GetCvTextAsync(accountId, request);
            if (string.IsNullOrWhiteSpace(cvText))
                throw new ArgumentException("Không có nội dung CV để phân tích. Vui lòng cung cấp CvId hoặc CvText.");

            // 4. Gọi Agent phân tích
            var result = await _cvAnalysisAgent.AnalyseCvAsync(cvText);

            // 5. Lưu cache vào DB (chỉ khi có cvId)
            if (request.CvId.HasValue && _cvDataProvider != null)
            {
                try
                {
                    var rawJson = JsonSerializer.Serialize(result);
                    await _cvDataProvider.SaveAnalysisResultAsync(accountId, request.CvId.Value, rawJson);
                    _logger.LogInformation("Cached CV analysis result for cvId {CvId}", request.CvId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache CV analysis result for cvId {CvId}", request.CvId.Value);
                }
            }

            return result;
        }

        // =====================================================================
        // VALIDATE CV IS IT
        // =====================================================================

        public async Task ValidateCvIsItAsync(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (ext == ".doc")
            {
                _logger.LogInformation("[CvValidate] .doc file — skipping IT validation, allowing upload.");
                return;
            }

            string cvText;
            try
            {
                cvText = ext == ".pdf"
                    ? await ExtractTextFromPdfAsync(file)
                    : await ExtractTextFromDocxAsync(file);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[CvValidate] Cannot extract text from file {FileName}", file.FileName);
                throw new ArgumentException("Không thể đọc nội dung file. Vui lòng kiểm tra lại file CV.");
            }

            if (string.IsNullOrWhiteSpace(cvText) || cvText.Length < 50)
            {
                _logger.LogWarning("[CvValidate] Extracted text too short ({Length} chars) for {FileName}", cvText?.Length ?? 0, file.FileName);
                throw new ArgumentException("File CV không có nội dung hoặc nội dung quá ngắn.");
            }

            _logger.LogInformation("[CvValidate] Extracted {Length} chars from {FileName}, calling AI...", cvText.Length, file.FileName);

            // Chỉ lấy 2000 ký tự đầu để tiết kiệm token
            var preview = cvText.Length > 2000 ? cvText.Substring(0, 2000) : cvText;
            var (isValid, reason) = await CheckIsItCvWithAiAsync(preview);

            if (!isValid)
            {
                _logger.LogInformation("[CvValidate] Rejected non-IT CV: {FileName}, Reason: {Reason}", file.FileName, reason);
                throw new ArgumentException("CV của bạn không hợp lệ hoặc không liên quan đến lĩnh vực IT.");
            }

            _logger.LogInformation("[CvValidate] IT CV validated OK: {FileName}", file.FileName);
        }

        private async Task<(bool IsValid, string? Reason)> CheckIsItCvWithAiAsync(string cvPreview)
        {
            const string systemPrompt =
                "Bạn là hệ thống kiểm tra CV tuyển dụng. " +
                "Nhiệm vụ duy nhất: xác định đoạn văn bản có phải trích từ CV thuộc lĩnh vực IT/công nghệ thông tin không. " +
                "IT bao gồm: lập trình, phần mềm, data, AI/ML, DevOps, network, cybersecurity, QA, BA, IT support, v.v. " +
                "Chỉ trả về JSON, không giải thích thêm.";

            var userPrompt = $@"Phân tích đoạn văn bản CV sau và trả về JSON:

                {cvPreview}

                Trả về ĐÚNG format JSON sau (không markdown, không giải thích):
                {{
                  ""is_it_cv"": <true hoặc false>,
                  ""reason"": ""<nếu false: lý do ngắn gọn bằng tiếng Việt, nếu true: null>""
                }}";

            string rawResponse;
            try
            {
                rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);
            }
            catch (Exception ex)
            {
                // Nếu AI lỗi → cho qua, không block upload vì lỗi hạ tầng
                _logger.LogWarning(ex, "[CvValidate] AI call failed, allowing upload by default.");
                return (true, null);
            }

            try
            {
                var cleaned = Regex.Replace(rawResponse.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
                cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "").Trim();

                using var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;

                var isItCv = root.TryGetProperty("is_it_cv", out var isItEl) && isItEl.GetBoolean();
                var reason = root.TryGetProperty("reason", out var reasonEl) && reasonEl.ValueKind != JsonValueKind.Null
                    ? reasonEl.GetString()
                    : null;

                return (isItCv, reason);
            }
            catch (Exception ex)
            {
                // Nếu parse lỗi → cho qua
                _logger.LogWarning(ex, "[CvValidate] Failed to parse AI response, allowing upload by default.");
                return (true, null);
            }
        }

        private static async Task<string> ExtractTextFromPdfAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var pdf = PdfDocument.Open(stream.ToArray());
            var sb = new StringBuilder();
            foreach (UglyToad.PdfPig.Content.Page page in pdf.GetPages())
                sb.AppendLine(page.Text);

            return sb.ToString();
        }

        private static async Task<string> ExtractTextFromDocxAsync(IFormFile file)
        {
            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Position = 0;

            using var wordDoc = WordprocessingDocument.Open(stream, false);
            var body = wordDoc.MainDocumentPart?.Document?.Body;
            return body?.InnerText ?? string.Empty;
        }

        private async Task<string> GetCvTextAsync(int accountId, AnalyseCvRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.CvText))
                return request.CvText;

            if (request.CvId.HasValue)
            {
                if (_cvDataProvider == null)
                    throw new InvalidOperationException("ICvDataProvider chưa được đăng ký. Không thể truy vấn CV từ database.");

                return await _cvDataProvider.GetCvTextAsync(accountId, request.CvId.Value);
            }

            throw new ArgumentException("Vui lòng cung cấp CvId hoặc CvText.");
        }
    }
}