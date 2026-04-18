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
            // Build prompts
            var systemPrompt = BuildSystemPrompt(request, cvContext, ragQuestions);
            var userPrompt = BuildUserPrompt(request, cvContext, ragQuestions);

            _logger.LogInformation(
                "Generating practice test: Type={TestType}, Field={Field}, Level={Level}, Questions={Count}, RAG={RagCount}",
                request.TestType, request.Field, request.Level, request.NumberOfQuestions, ragQuestions.Count);

            // Gọi Gemini
            var rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);
            var result = ParseResponse(rawResponse, request);

            _logger.LogInformation("Practice test generated: {Title}, {Count} questions",
                result.TestTitle, result.TotalQuestions);

            return result;
        }

        // ── Private helpers ──

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