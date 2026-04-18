using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Agents
{
    /// <summary>
    /// Agent tạo feedback phỏng vấn (Tầng 3 - Agents)
    /// Chịu trách nhiệm: build prompt feedback, gọi AI Service, parse response
    /// </summary>
    public class FeedbackAgent : IFeedbackAgent
    {
        private readonly IGeminiService _geminiService;
        private readonly ILogger<FeedbackAgent> _logger;

        private static readonly string _feedbackSystemPrompt;
        private static readonly string _feedbackSummarySystemPrompt;

        static FeedbackAgent()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var feedbackPromptPath = Path.Combine(basePath, "SystemMessages", "interview-feedback-system.txt");
            var feedbackSummaryPromptPath = Path.Combine(basePath, "SystemMessages", "interview-feedback-summary-system.txt");

            _feedbackSystemPrompt = File.Exists(feedbackPromptPath)
                ? File.ReadAllText(feedbackPromptPath)
                : "Bạn là chuyên gia đánh giá phỏng vấn IT. Đánh giá câu trả lời và trả về JSON.";
            _feedbackSummarySystemPrompt = File.Exists(feedbackSummaryPromptPath)
                ? File.ReadAllText(feedbackSummaryPromptPath)
                : "Bạn là chuyên gia phỏng vấn IT. Hãy tổng kết buổi phỏng vấn và trả về JSON gồm overall_comment, strengths, improvements.";
        }

        public FeedbackAgent(IGeminiService geminiService, ILogger<FeedbackAgent> logger)
        {
            _geminiService = geminiService;
            _logger = logger;
        }

        public async Task<FeedbackResult> GeneratePerQuestionFeedbackAsync(InterviewResponseData response, string? gapAnalysis = null)
        {
            var userPrompt = BuildFeedbackUserPrompt(response, gapAnalysis);
            var rawFeedback = await _geminiService.GenerateContentAsync(_feedbackSystemPrompt, userPrompt);
            return ParseFeedbackResponse(rawFeedback);
        }

        public async Task<string> GenerateSessionSummaryAsync(List<InterviewResponseData> answeredResponses, double overallAvg)
        {
            _logger.LogInformation("Generating OVERALL JSON summary");
            var sb = new StringBuilder();
            sb.AppendLine("=== TỔNG KẾT PHIÊN PHỎNG VẤN ===");
            sb.AppendLine($"Tổng số câu hỏi hoàn thành: {answeredResponses.Count}");
            sb.AppendLine($"Điểm trung bình (Average Score): {overallAvg:F2}/1.00");
            sb.AppendLine();
            foreach (var r in answeredResponses)
            {
                sb.AppendLine($"Hỏi: {r.QuestionContent}");
                sb.AppendLine($"Trả lời: {r.UserAnswer}");
                sb.AppendLine($"Nhận xét AI: {r.AIFeedback}");
                sb.AppendLine();
            }
            sb.AppendLine("Dựa vào toàn bộ lịch sử Q&A trên, hãy tổng kết và đưa ra nhận xét chung cho ứng viên theo đúng định dạng JSON yêu cầu.");

            try
            {
                var rawSummary = await _geminiService.GenerateContentAsync(_feedbackSummarySystemPrompt, sb.ToString());
                return CleanJsonResponse(rawSummary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate Overall JSON Summary");
                var fallbackJson = new
                {
                    overall_comment = $"Bạn đã hoàn thành {answeredResponses.Count} câu hỏi. Điểm trung bình tổng thể: {overallAvg:F2}/1.00. Hãy xem chi tiết feedback cho từng câu hỏi để cải thiện kỹ năng của bạn.",
                    strengths = new List<string>(),
                    improvements = new List<string>()
                };
                return JsonSerializer.Serialize(fallbackJson);
            }
        }

        // ── Private helpers ──

        private static string BuildFeedbackUserPrompt(InterviewResponseData response, string? gapAnalysis = null)
        {
            string currentPhase = response.TurnNumber switch
            {
                <= 2 => "Giai đoạn 1: Giới thiệu bản thân (Ice-breaker)",
                <= 4 => "Giai đoạn 2: Câu hỏi kỹ thuật chuyên môn (Technical)",
                <= 7 => "Giai đoạn 3: Tình huống giả định (Situational)",
                9 => "Giai đoạn 4: Đào sâu tình huống từ câu trả lời trước (Deep-dive)",
                _ => "Giai đoạn 5: Văn hóa làm việc và mức độ phù hợp (Culture fit)"
            };

            var sb = new StringBuilder();
            sb.AppendLine("=== ĐÁNH GIÁ CÂU TRẢ LỜI ===");
            if (!string.IsNullOrEmpty(gapAnalysis))
            {
                sb.AppendLine("\n=== CONTEXT: KHOẢNG TRỐNG NĂNG LỰC (GAP ANALYSIS) ===");
                sb.AppendLine("Dưới đây là các điểm thiếu hụt của ứng viên so với JD. Hãy sử dụng thông tin này để đưa ra nhận xét mang tính huấn luyện (coaching) để giúp ứng viên lấp đầy gap.");
                sb.AppendLine(gapAnalysis);
                sb.AppendLine("=== HẾT CONTEXT GAP ===\n");
            }
            sb.AppendLine($"Bạn hãy phân tích theo tiêu chí của: [{currentPhase}]");
            sb.AppendLine($"Câu hỏi: {response.QuestionContent}");
            sb.AppendLine($"Câu trả lời: {response.UserAnswer}");
            if (!string.IsNullOrEmpty(response.ExpectedAnswerOutline))
                sb.AppendLine($"Gợi ý đáp án: {response.ExpectedAnswerOutline}");
            if (response.ExpectedBloomLevel.HasValue)
                sb.AppendLine($"Bloom Level mong đợi: {response.ExpectedBloomLevel}");
            sb.AppendLine("\nHãy đánh giá câu trả lời trên và trả về JSON feedback.");
            return sb.ToString();
        }

        private FeedbackResult ParseFeedbackResponse(string rawResponse)
        {
            var cleaned = CleanJsonResponse(rawResponse);
            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;
                var result = new FeedbackResult
                {
                    OverallComment = root.TryGetProperty("overallComment", out var oc) ? oc.GetString() ?? "" : "",
                    SuggestedAnswer = root.TryGetProperty("suggestedAnswer", out var sa) ? sa.GetString() : null
                };
                if (root.TryGetProperty("strengths", out var strengths))
                    result.Strengths = strengths.EnumerateArray().Select(s => s.GetString() ?? "").ToList();
                if (root.TryGetProperty("improvements", out var improvements))
                    result.Improvements = improvements.EnumerateArray().Select(s => s.GetString() ?? "").ToList();
                if (root.TryGetProperty("scores", out var scores))
                {
                    if (scores.TryGetProperty("bloomScore", out var bs)) result.BloomScore = bs.GetDouble();
                    if (scores.TryGetProperty("demonstratedBloomLevel", out var dbl)) result.DemonstratedBloomLevel = dbl.GetInt32();
                    if (scores.TryGetProperty("technicalDepthScore", out var tds)) result.TechnicalDepthScore = tds.GetDouble();
                    if (scores.TryGetProperty("problemSolvingScore", out var pss)) result.ProblemSolvingScore = pss.GetDouble();
                    if (scores.TryGetProperty("communicationScore", out var cs)) result.CommunicationScore = cs.GetDouble();
                    if (scores.TryGetProperty("practicalExperienceScore", out var pes)) result.PracticalExperienceScore = pes.GetDouble();
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse feedback response");
                return new FeedbackResult { OverallComment = "Không thể phân tích feedback." };
            }
        }

        private static string CleanJsonResponse(string text)
        {
            var cleaned = Regex.Replace(text.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "");
            return cleaned.Trim();
        }
    }
}