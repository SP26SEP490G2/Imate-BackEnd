using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Imate.AI.Module.Services
{
    /// <summary>
    /// Practice Test Service - sinh bài test luyện tập bằng Gemini AI + RAG từ Question Bank
    /// UC-30: Practice Test
    /// </summary>
    public class PracticeTestService : IPracticeTestService
    {
        private readonly IGeminiService _geminiService;
        private readonly ICvDataProvider? _cvDataProvider;
        private readonly IQuestionDataProvider? _questionDataProvider;
        private readonly ILogger<PracticeTestService> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PracticeTestService(
            IGeminiService geminiService,
            ILogger<PracticeTestService> logger,
            ICvDataProvider? cvDataProvider = null,
            IQuestionDataProvider? questionDataProvider = null)
        {
            _geminiService = geminiService;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
            _questionDataProvider = questionDataProvider;
        }

        public async Task<PracticeTestResponse> GenerateTestAsync(int accountId, GeneratePracticeTestRequest request)
        {
            ValidateRequest(request);

            // 1. Lấy CV context nếu cần
            string? cvContext = null;
            if (request.UseCV && !string.IsNullOrWhiteSpace(request.CvText))
            {
                cvContext = request.CvText;
            }

            // 2. RAG: Lấy câu hỏi mẫu từ Question Bank trong DB
            List<QuestionBankItem> ragQuestions = new();
            if (_questionDataProvider != null)
            {
                try
                {
                    ragQuestions = await _questionDataProvider.GetQuestionsAsync(
                        request.Field, request.Level, request.NumberOfQuestions);
                    _logger.LogInformation(
                        "RAG: Retrieved {Count} reference questions from Question Bank for field={Field}, level={Level}",
                        ragQuestions.Count, request.Field, request.Level);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RAG: Failed to retrieve questions from DB, falling back to AI-only mode");
                }
            }
            else
            {
                _logger.LogInformation("RAG: IQuestionDataProvider not registered, using AI-only mode");
            }

            // 3. Build prompts
            var systemPrompt = BuildSystemPrompt(request, cvContext, ragQuestions);
            var userPrompt = BuildUserPrompt(request, cvContext, ragQuestions);
            _logger.LogInformation("System Prompt: {SystemPrompt}", systemPrompt);
            _logger.LogInformation("User Prompt: {UserPrompt}", userPrompt);
            _logger.LogInformation(
                "Generating practice test: Type={TestType}, Field={Field}, Level={Level}, Questions={Count}, RAG={RagCount}",
                request.TestType, request.Field, request.Level, request.NumberOfQuestions, ragQuestions.Count);

            // 4. Gọi Gemini
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

        private static string BuildSystemPrompt(GeneratePracticeTestRequest request, string? cvContext, List<QuestionBankItem> ragQuestions)
        {
            var testTypeDesc = request.TestType == "Language"
                ? "đánh giá năng lực ngoại ngữ (Tiếng Anh/Tiếng Nhật) trong bối cảnh IT"
                : "kiến thức chuyên môn kỹ thuật IT";

            var sb = new StringBuilder();

            sb.AppendLine($@"Bạn là một chuyên gia tuyển dụng IT và giáo dục công nghệ với hơn 15 năm kinh nghiệm.
Nhiệm vụ: Sinh bài test {testTypeDesc} cho vị trí {request.Field} cấp bậc {request.Level}.

Yêu cầu:
1. Tạo đúng {request.NumberOfQuestions} câu hỏi trắc nghiệm (4 đáp án A, B, C, D)
2. Độ khó phù hợp với cấp bậc {request.Level}
3. Câu hỏi phải thực tế, liên quan đến công việc {request.Field}
4. Mỗi câu hỏi cần có giải thích đáp án đúng
5. Chủ đề đa dạng, bao phủ nhiều khía cạnh của {request.Field}");

            if (request.TestType == "Language")
            {
                sb.AppendLine(@"6. Câu hỏi về ngữ pháp, từ vựng IT, reading comprehension, và giao tiếp trong môi trường IT
7. Bao gồm cả tiếng Anh kỹ thuật và giao tiếp hàng ngày trong công ty IT");
            }
            else
            {
                sb.AppendLine($@"6. Bao gồm câu hỏi lý thuyết, best practices, và tình huống thực tế
7. Phù hợp với stack công nghệ phổ biến cho {request.Field}");
            }

            if (!string.IsNullOrWhiteSpace(cvContext))
            {
                sb.AppendLine("8. CÁ NHÂN HÓA: Dựa vào CV của ứng viên để tạo câu hỏi phù hợp với kinh nghiệm và kỹ năng của họ");
            }

            // RAG: Inject câu hỏi mẫu từ DB
            if (ragQuestions.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("=== DỮ LIỆU THAM KHẢO TỪ NGÂN HÀNG CÂU HỎI ===");
                sb.AppendLine("Hãy DỰA VÀO các câu hỏi mẫu dưới đây để tạo câu hỏi trắc nghiệm.");
                sb.AppendLine("Bạn có thể biến đổi, mở rộng, hoặc tạo câu hỏi tương tự nhưng ở dạng trắc nghiệm 4 đáp án.");
                sb.AppendLine("KHÔNG copy nguyên văn — hãy paraphrase và tạo đáp án nhiễu hợp lý.");
                sb.AppendLine();

                for (int i = 0; i < ragQuestions.Count; i++)
                {
                    var q = ragQuestions[i];
                    sb.AppendLine($"--- Câu hỏi tham khảo {i + 1} ---");
                    sb.AppendLine($"Nội dung: {q.Content}");
                    if (!string.IsNullOrWhiteSpace(q.SampleAnswer))
                        sb.AppendLine($"Đáp án mẫu: {q.SampleAnswer}");
                    sb.AppendLine($"Độ khó: {q.Difficulty}");
                    if (q.Skills.Count > 0)
                        sb.AppendLine($"Skills: {string.Join(", ", q.Skills)}");
                    if (q.Categories.Count > 0)
                        sb.AppendLine($"Categories: {string.Join(", ", q.Categories)}");
                    sb.AppendLine();
                }

                sb.AppendLine("=== HẾT DỮ LIỆU THAM KHẢO ===");
                sb.AppendLine();
            }

            sb.AppendLine($@"
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
}}");

            return sb.ToString();
        }

        private static string BuildUserPrompt(GeneratePracticeTestRequest request, string? cvContext, List<QuestionBankItem> ragQuestions)
        {
            var sb = new StringBuilder();

            if (ragQuestions.Count > 0)
            {
                sb.AppendLine($"Dựa vào {ragQuestions.Count} câu hỏi tham khảo từ ngân hàng câu hỏi, hãy tạo bài test {request.TestType} cho vị trí {request.Field}, cấp bậc {request.Level}, gồm {request.NumberOfQuestions} câu hỏi trắc nghiệm.");
                sb.AppendLine("Chuyển đổi các câu hỏi tham khảo thành dạng trắc nghiệm 4 đáp án với đáp án nhiễu hợp lý.");
            }
            else
            {
                sb.AppendLine($"Hãy tạo bài test {request.TestType} cho vị trí {request.Field}, cấp bậc {request.Level}, gồm {request.NumberOfQuestions} câu hỏi trắc nghiệm.");
            }

            if (!string.IsNullOrWhiteSpace(cvContext))
            {
                sb.AppendLine($"\nCV của ứng viên:\n{cvContext}");
            }

            sb.AppendLine("\nTrả về kết quả dưới dạng JSON.");
            return sb.ToString();
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
