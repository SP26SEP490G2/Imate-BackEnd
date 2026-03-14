using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Imate.AI.Module.Services
{
    /// <summary>
    /// Practice Test Service - sinh bài test luyện tập bằng Gemini AI
    /// UC-30: Practice Test
    /// </summary>
    public class PracticeTestService : IPracticeTestService
    {
        private readonly IGeminiService _geminiService;
        private readonly ICvDataProvider? _cvDataProvider;
        private readonly ILogger<PracticeTestService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PracticeTestService(
            IGeminiService geminiService,
            ILogger<PracticeTestService> logger,
            ICvDataProvider? cvDataProvider = null)
        {
            _geminiService = geminiService;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
        }

        public async Task<PracticeTestResponse> GenerateTestAsync(int accountId, GeneratePracticeTestRequest request)
        {
            ValidateRequest(request);

            string? cvContext = null;
            if (request.UseCV && !string.IsNullOrWhiteSpace(request.CvText))
            {
                cvContext = request.CvText;
            }

            var systemPrompt = BuildSystemPrompt(request, cvContext);
            var userPrompt = BuildUserPrompt(request, cvContext);

            _logger.LogInformation(
                "Generating practice test: Type={TestType}, Field={Field}, Level={Level}, Questions={Count}",
                request.TestType, request.Field, request.Level, request.NumberOfQuestions);

            var rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);
            var result = ParseResponse(rawResponse, request);

            _logger.LogInformation("Practice test generated: {Title}, {Count} questions",
                result.TestTitle, result.TotalQuestions);

            return result;
        }

        private static void ValidateRequest(GeneratePracticeTestRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Field))
                throw new ArgumentException("Vui lòng chọn lĩnh vực chuyên môn.");

            if (string.IsNullOrWhiteSpace(request.Level))
                throw new ArgumentException("Vui lòng chọn cấp bậc ứng tuyển.");

            if (request.NumberOfQuestions < 5 || request.NumberOfQuestions > 20)
                request.NumberOfQuestions = 10;
        }

        private static string BuildSystemPrompt(GeneratePracticeTestRequest request, string? cvContext)
        {
            var testTypeDesc = request.TestType == "Language"
                ? "đánh giá năng lực ngoại ngữ (Tiếng Anh/Tiếng Nhật) trong bối cảnh IT"
                : "kiến thức chuyên môn kỹ thuật IT";

            var prompt = $@"Bạn là một chuyên gia tuyển dụng IT và giáo dục công nghệ với hơn 15 năm kinh nghiệm.
Nhiệm vụ: Sinh bài test {testTypeDesc} cho vị trí {request.Field} cấp bậc {request.Level}.

Yêu cầu:
1. Tạo đúng {request.NumberOfQuestions} câu hỏi trắc nghiệm (4 đáp án A, B, C, D)
2. Độ khó phù hợp với cấp bậc {request.Level}
3. Câu hỏi phải thực tế, liên quan đến công việc {request.Field}
4. Mỗi câu hỏi cần có giải thích đáp án đúng
5. Chủ đề đa dạng, bao phủ nhiều khía cạnh của {request.Field}";

            if (request.TestType == "Language")
            {
                prompt += @"
6. Câu hỏi về ngữ pháp, từ vựng IT, reading comprehension, và giao tiếp trong môi trường IT
7. Bao gồm cả tiếng Anh kỹ thuật và giao tiếp hàng ngày trong công ty IT";
            }
            else
            {
                prompt += $@"
6. Bao gồm câu hỏi lý thuyết, best practices, và tình huống thực tế
7. Phù hợp với stack công nghệ phổ biến cho {request.Field}";
            }

            if (!string.IsNullOrWhiteSpace(cvContext))
            {
                prompt += @"
8. CÁ NHÂN HÓA: Dựa vào CV của ứng viên để tạo câu hỏi phù hợp với kinh nghiệm và kỹ năng của họ";
            }

            prompt += $@"

PHẢI trả về ĐÚNG JSON format sau (không markdown, không code block, CHỈ JSON thuần):
{{
  ""testTitle"": ""<tiêu đề bài test>"",
  ""testType"": ""{request.TestType}"",
  ""field"": ""{request.Field}"",
  ""level"": ""{request.Level}"",
  ""totalQuestions"": {request.NumberOfQuestions},
  ""timeLimitMinutes"": <thời gian làm bài tính bằng phút>,
  ""questions"": [
    {{
      ""id"": 1,
      ""questionText"": ""<nội dung câu hỏi>"",
      ""options"": [
        {{ ""label"": ""A"", ""text"": ""<đáp án A>"" }},
        {{ ""label"": ""B"", ""text"": ""<đáp án B>"" }},
        {{ ""label"": ""C"", ""text"": ""<đáp án C>"" }},
        {{ ""label"": ""D"", ""text"": ""<đáp án D>"" }}
      ],
      ""correctAnswer"": ""<A|B|C|D>"",
      ""explanation"": ""<giải thích tại sao đáp án đúng>""
    }}
  ]
}}";

            return prompt;
        }

        private static string BuildUserPrompt(GeneratePracticeTestRequest request, string? cvContext)
        {
            var prompt = $"Hãy tạo bài test {request.TestType} cho vị trí {request.Field}, cấp bậc {request.Level}, gồm {request.NumberOfQuestions} câu hỏi trắc nghiệm.";

            if (!string.IsNullOrWhiteSpace(cvContext))
            {
                prompt += $"\n\nCV của ứng viên:\n{cvContext}";
            }

            prompt += "\n\nTrả về kết quả dưới dạng JSON.";
            return prompt;
        }

        private PracticeTestResponse ParseResponse(string responseText, GeneratePracticeTestRequest request)
        {
            var cleaned = Regex.Replace(responseText.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "");
            cleaned = cleaned.Trim();

            try
            {
                var result = JsonSerializer.Deserialize<PracticeTestResponse>(cleaned, JsonOptions);
                if (result == null)
                    throw new Exception("Parsed result is null");

                // Ensure metadata is correct
                result.TestType = request.TestType;
                result.Field = request.Field;
                result.Level = request.Level;
                result.TotalQuestions = result.Questions.Count;

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse practice test response: {Response}",
                    cleaned.Substring(0, Math.Min(500, cleaned.Length)));
                throw new Exception("Không thể phân tích phản hồi từ AI. Vui lòng thử lại.", ex);
            }
        }
    }
}
