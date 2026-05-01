using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Agents
{
    /// <summary>
    /// Agent tạo bài test luyện tập (Tầng 3 - Agents)
    /// Chịu trách nhiệm: build prompt, gọi AI Service, parse response
    /// Nhận dữ liệu đã chuẩn bị từ Orchestrator (cvContext, ragQuestions)
    /// </summary>
    public class PracticeTestAgent : IPracticeTestAgent
    {
        private readonly IGeminiService _geminiService;
        private readonly ILogger<PracticeTestAgent> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PracticeTestAgent(IGeminiService geminiService, ILogger<PracticeTestAgent> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<PracticeTestResponse> GenerateTestAsync(
            GeneratePracticeTestRequest request,
            string? cvContext,
            List<QuestionBankItem> ragQuestions)
        {
            var systemPrompt = BuildSystemPrompt(request, cvContext, ragQuestions);
            var userPrompt = BuildUserPrompt(request, cvContext, ragQuestions);

            _logger.LogInformation(
                "Generating practice test: Type={TestType}, Field={Field}, Level={Level}, Questions={Count}, RAG={RagCount}",
                request.TestType, request.Field, request.Level, request.NumberOfQuestions, ragQuestions.Count);

            // Retry tự động nếu AI trả về JSON bị lỗi cú pháp
            const int maxAttempts = 3;
            Exception? lastException = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);
                    var result = ParseResponse(rawResponse, request);

                    _logger.LogInformation(
                        "Practice test generated (attempt {Attempt}): {Title}, {Count} questions",
                        attempt, result.TestTitle, result.TotalQuestions);

                    return result;
                }
                catch (Exception ex) when (ex.InnerException is System.Text.Json.JsonException || ex.Message.Contains("phân tích"))
                {
                    lastException = ex;
                    _logger.LogWarning(
                        "[PracticeTest] Attempt {Attempt}/{Max} failed to parse AI response. Retrying...",
                        attempt, maxAttempts);

                    if (attempt < maxAttempts)
                        await Task.Delay(500); // Đợi 0.5s trước retry
                }
            }

            throw new Exception($"AI không thể sinh bài test hợp lệ sau {maxAttempts} lần thử. Vui lòng thử lại.", lastException);
        }

        // ── Private helpers ──

        private static string BuildSystemPrompt(GeneratePracticeTestRequest request, string? cvContext, List<QuestionBankItem> ragQuestions)
        {
            var testTypeDesc = request.TestType == "Language"
                ? "đánh giá năng lực ngoại ngữ (Tiếng Anh/Tiếng Nhật) trong bối cảnh IT"
                : "kiến thức chuyên môn kỹ thuật IT";

            var sb = new StringBuilder();

            var skillInfo = !string.IsNullOrWhiteSpace(request.Skill)
                ? $", tập trung vào kỹ năng {request.Skill}"
                : "";

            sb.AppendLine($@"Bạn là một chuyên gia tuyển dụng IT và giáo dục công nghệ với hơn 15 năm kinh nghiệm.
Nhiệm vụ: Sinh bài test {testTypeDesc} cho vị trí {request.Field} cấp bậc {request.Level}{skillInfo}.

Yêu cầu:
1. Tạo đúng {request.NumberOfQuestions} câu hỏi trắc nghiệm (4 đáp án A, B, C, D)
2. Độ khó phù hợp với cấp bậc {request.Level}
3. Câu hỏi phải thực tế, liên quan đến công việc {request.Field}{(string.IsNullOrWhiteSpace(request.Skill) ? "" : $" với kỹ năng {request.Skill}")}
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
  ""skill"": ""{request.Skill}"",
  ""level"": ""{request.Level}"",
  ""totalQuestions"": {request.NumberOfQuestions},
  ""timeLimitMinutes"": 30,
  ""questions"": [
    {{
      ""id"": 1,
      ""questionText"": ""<nội dung câu hỏi>"",
      ""options"": [
        {{ ""label"": ""A"", ""text"": ""<chỉ nội dung đáp án, KHÔNG có chữ A hay dấu chấm đằng đầu>"" }},
        {{ ""label"": ""B"", ""text"": ""<chỉ nội dung đáp án, KHÔNG có chữ B hay dấu chấm đằng đầu>"" }},
        {{ ""label"": ""C"", ""text"": ""<chỉ nội dung đáp án, KHÔNG có chữ C hay dấu chấm đằng đầu>"" }},
        {{ ""label"": ""D"", ""text"": ""<chỉ nội dung đáp án, KHÔNG có chữ D hay dấu chấm đằng đầu>"" }}
      ],
      ""correctAnswer"": ""<A hoặc B hoặc C hoặc D, chỉ 1 ký tự>"",
      ""explanation"": ""<giải thích ngắn gọn tại sao đáp án đúng>""
    }}
  ]
}}

QUY TẪC BẮT BUỘC:
- trường ""text"" trong options CHỈ chứa nội dung đáp án, KHÔNG bắt đầu bằng ""A."", ""B."", ""C."", ""D."" hay ""A:"", ""B:""
- KHÔNG đặt dấu ngoặc kép ("") bên trong giá trị chuỗi — dùng tên thay thế
- JSON phải hợp lệ 100%, không có trailing comma");

            return sb.ToString();
        }

        private static string BuildUserPrompt(GeneratePracticeTestRequest request, string? cvContext, List<QuestionBankItem> ragQuestions)
        {
            var sb = new StringBuilder();

            var skillPart = !string.IsNullOrWhiteSpace(request.Skill)
                ? $", kỹ năng {request.Skill}"
                : "";

            if (ragQuestions.Count > 0)
            {
                sb.AppendLine($"Dựa vào {ragQuestions.Count} câu hỏi tham khảo từ ngân hàng câu hỏi, hãy tạo bài test {request.TestType} cho vị trí {request.Field}{skillPart}, cấp bậc {request.Level}, gồm {request.NumberOfQuestions} câu hỏi trắc nghiệm.");
                sb.AppendLine("Chuyển đổi các câu hỏi tham khảo thành dạng trắc nghiệm 4 đáp án với đáp án nhiễu hợp lý.");
            }
            else
            {
                sb.AppendLine($"Hãy tạo bài test {request.TestType} cho vị trí {request.Field}{skillPart}, cấp bậc {request.Level}, gồm {request.NumberOfQuestions} câu hỏi trắc nghiệm.");
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
            var cleaned = CleanJsonResponse(responseText);

            try
            {
                var result = JsonSerializer.Deserialize<PracticeTestResponse>(cleaned, JsonOptions);
                if (result == null)
                    throw new JsonException("Parsed result is null");

                // Ensure metadata is correct — override AI values with known-good values
                result.TestType = request.TestType;
                result.Field = request.Field;
                result.Skill = request.Skill;
                result.Level = request.Level;
                result.TotalQuestions = result.Questions.Count;
                result.TimeLimitMinutes = 30; // Luôn ép 30 phút, không để AI tự quyết

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "[PracticeTest] JSON parse failed. Raw AI response (first 1000 chars):\n{Raw}",
                    cleaned.Substring(0, Math.Min(1000, cleaned.Length)));
                throw new Exception("Không thể phân tích phản hồi từ AI. Vui lòng thử lại.", ex);
            }
        }

        /// <summary>
        /// Làm sạch response từ AI: xóa markdown code block, sau đó tìm đúng boundaries của JSON object.
        /// </summary>
        private static string CleanJsonResponse(string raw)
        {
            // 1. Xóa markdown code block (```json ... ```)
            var cleaned = Regex.Replace(raw.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "").Trim();

            // 2. Tìm vị trí { đầu tiên và } cuối cùng để extract JSON thuần
            //    Xử lý trường hợp AI thêm text trước/sau JSON object
            var firstBrace = cleaned.IndexOf('{');
            var lastBrace = cleaned.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                cleaned = cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);
            }

            return cleaned;
        }
    }
}