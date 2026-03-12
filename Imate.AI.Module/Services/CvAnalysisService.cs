using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
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
        private readonly ILogger<CvAnalysisService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private const string SystemPrompt = @"Bạn là một chuyên gia tuyển dụng IT cấp cao với hơn 15 năm kinh nghiệm.
Nhiệm vụ: Phân tích CV ứng viên IT và đưa ra đánh giá chi tiết.

Yêu cầu:
1. Đánh giá tổng thể CV trên thang 100 điểm
2. Xác định vị trí công việc phù hợp nhất
3. Đánh giá mức độ phù hợp với thị trường (""Cao"", ""Trung bình"", hoặc ""Thấp"")
4. Liệt kê 3 điểm mạnh nổi bật (với mô tả ngắn gọn)
5. Liệt kê 3 điểm cần cải thiện (với mô tả ngắn gọn)
6. Gợi ý 4 câu hỏi phỏng vấn dựa trên CV, mỗi câu thuộc một danh mục khác nhau

PHẢI trả về ĐÚNG JSON format sau (không markdown, không code block, không giải thích thêm, CHỈ JSON thuần):
{
  ""score"": <number 0-100>,
  ""candidateName"": ""<tên ứng viên>"",
  ""jobTitle"": ""<vị trí phù hợp>"",
  ""marketFit"": ""<Cao|Trung bình|Thấp>"",
  ""strengths"": [
    { ""title"": ""<tiêu đề>"", ""description"": ""<mô tả>"" }
  ],
  ""improvements"": [
    { ""title"": ""<tiêu đề>"", ""description"": ""<mô tả>"" }
  ],
  ""interviewQuestions"": [
    { ""category"": ""<danh mục viết hoa>"", ""question"": ""<câu hỏi>"" }
  ]
}";

        public CvAnalysisService(
            IGeminiService geminiService,
            ILogger<CvAnalysisService> logger,
            ICvDataProvider? cvDataProvider = null)
        {
            _geminiService = geminiService;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
        }

        public async Task<CvAnalysisResponse> AnalyseCvAsync(int accountId, AnalyseCvRequest request)
        {
            string cvText = await GetCvTextAsync(accountId, request);

            if (string.IsNullOrWhiteSpace(cvText))
            {
                throw new ArgumentException("Không có nội dung CV để phân tích. Vui lòng cung cấp CvId hoặc CvText.");
            }

            _logger.LogInformation("Starting CV analysis for account {AccountId}", accountId);
            var userPrompt = $"Hãy phân tích CV sau đây và trả về kết quả dưới dạng JSON:\n\n{cvText}";
            var rawResponse = await _geminiService.GenerateContentAsync(SystemPrompt, userPrompt);

            var result = ParseGeminiResponse(rawResponse);
            _logger.LogInformation("CV analysis completed. Score: {Score}, Candidate: {Name}", result.Score, result.CandidateName);

            return result;
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
