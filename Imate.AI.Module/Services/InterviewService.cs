using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;

namespace Imate.AI.Module.Services
{
    /// <summary>
    /// Service phỏng vấn AI — UC-35: Practice Mock Interview
    /// Sử dụng Gemini AI để tạo câu hỏi adaptive và đánh giá câu trả lời
    /// </summary>
    public class InterviewService : IInterviewService
    {
        private readonly IGeminiService _geminiService;
        private readonly IInterviewSessionDataProvider _dataProvider;
        private readonly ILogger<InterviewService> _logger;
        private readonly IQuestionDataProvider _questionDataProvider;

        private const int MaxQuestionsPerSession = 10;
        private static readonly string _questionSystemPrompt;
        private static readonly string _feedbackSystemPrompt;

        static InterviewService()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var questionPromptPath = Path.Combine(basePath, "SystemMessages", "interview-question-system.txt");
            var feedbackPromptPath = Path.Combine(basePath, "SystemMessages", "interview-feedback-system.txt");

            _questionSystemPrompt = File.Exists(questionPromptPath)
                ? File.ReadAllText(questionPromptPath)
                : "Bạn là chuyên gia phỏng vấn IT. Tạo câu hỏi phỏng vấn và trả về JSON.";

            _feedbackSystemPrompt = File.Exists(feedbackPromptPath)
                ? File.ReadAllText(feedbackPromptPath)
                : "Bạn là chuyên gia đánh giá phỏng vấn IT. Đánh giá câu trả lời và trả về JSON.";
        }

        public InterviewService(
            IGeminiService geminiService,
            IInterviewSessionDataProvider dataProvider,
            ILogger<InterviewService> logger,
            IQuestionDataProvider questionDataProvider)
        {
            _geminiService = geminiService;
            _dataProvider = dataProvider;
            _logger = logger;
            _questionDataProvider = questionDataProvider;
        }

        public async Task<string> GenerateWelcomeMessageAsync(string? CvContent, string? positionName, string? companyName, string? language = null)
        {
            var lang = language ?? "vi-VN";
            var systemPrompt = "Bạn là phỏng vấn viên AI tên Bernie, chuyên phỏng vấn IT. Hãy tạo lời chào mừng ngắn gọn, thân thiện, chuyên nghiệp cho buổi phỏng vấn. Trả về text thuần, KHÔNG trả JSON.";

            var sb = new StringBuilder();
            sb.AppendLine("Hãy tạo lời chào mừng cho buổi phỏng vấn với thông tin:");
            if (!string.IsNullOrEmpty(positionName)) sb.AppendLine($"- Vị trí: {positionName}");
            if (!string.IsNullOrEmpty(companyName)) sb.AppendLine($"- Công ty: {companyName}");
            if (!string.IsNullOrEmpty(CvContent)) sb.AppendLine($"- Cv User: {CvContent}");
            sb.AppendLine($"- Ngôn ngữ: {(lang.StartsWith("vi") ? "Tiếng Việt" : "English")}");
            sb.AppendLine("Giới thiệu bản thân là Bernie, giải thích ngắn gọn quy trình phỏng vấn. Tối đa 3-4 câu.");

            var welcomeMessage = await _geminiService.GenerateContentAsync(systemPrompt, sb.ToString());
            return welcomeMessage.Trim();
        }

        public async Task<GenerateQuestionResult> GenerateQuestionAsync(int sessionId, double? estimatedAbility = null)
        {
            var session = await _dataProvider.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên phỏng vấn {sessionId}");

            var existingResponses = await _dataProvider.GetResponsesBySessionIdAsync(sessionId);
            var answeredCount = existingResponses.Count(r => !string.IsNullOrEmpty(r.UserAnswer));
            var ragQuestions = await GetReferenceQuestionsAsync(session.SkillName ?? "General", session.LevelName ?? "Junior");

            if (answeredCount >= MaxQuestionsPerSession)
            {
                return new GenerateQuestionResult
                {
                    IsTerminated = true,
                    TerminationReason = "MaxQuestionsReached",
                    TerminationMessage = $"Buổi phỏng vấn đã hoàn thành {MaxQuestionsPerSession} câu hỏi. Cảm ơn bạn đã tham gia! Hệ thống đang tạo báo cáo phản hồi..."
                };
            }

            var userPrompt = BuildQuestionUserPrompt(session, existingResponses, estimatedAbility, ragQuestions);

            _logger.LogInformation("Generating question {Number}/{Max} for session {SessionId}",
                existingResponses.Count + 1, MaxQuestionsPerSession, sessionId);
            var delay = Random.Shared.Next(800, 1500);
            await Task.Delay(delay);
            var rawResponse = await _geminiService.GenerateContentAsync(_questionSystemPrompt, userPrompt);
            var questionData = ParseQuestionResponse(rawResponse);

            // Lưu câu hỏi vào DB
            var newResponse = new InterviewResponseData
            {
                InterviewSessionId = sessionId,
                TurnNumber = existingResponses.Count + 1,
                QuestionContent = questionData.QuestionText,
                ExpectedAnswerOutline = questionData.ExpectedAnswerOutline,
                ExpectedBloomLevel = questionData.Metrics?.BloomTaxonomy?.Level,
                DifficultyScore = questionData.Metrics?.Irt?.DifficultyScore,
                CognitiveLoadScore = questionData.Metrics?.Clt?.TotalCognitiveLoad
            };

            var savedId = await _dataProvider.CreateResponseAsync(newResponse);
            questionData.InterviewResponseId = savedId;

            _logger.LogInformation("Question generated: ResponseId={ResponseId}, Topic={Topic}",
                savedId, questionData.Topic);

            return questionData;
        }

        public async Task<string> GenerateFeedbackForSessionAsync(int sessionId)
        {
            var responses = await _dataProvider.GetResponsesBySessionIdAsync(sessionId);
            var answeredResponses = responses
                .Where(r => !string.IsNullOrEmpty(r.UserAnswer))
                .OrderBy(r => r.TurnNumber)
                .ToList();

            _logger.LogInformation("Generating feedback for {Count} answers in session {SessionId}",
                answeredResponses.Count, sessionId);

            var totalScores = new List<double>();

            foreach (var response in answeredResponses)
            {
                try
                {
                    var userPrompt = BuildFeedbackUserPrompt(response);
                    var rawFeedback = await _geminiService.GenerateContentAsync(_feedbackSystemPrompt, userPrompt);
                    var feedback = ParseFeedbackResponse(rawFeedback);

                    response.AIFeedback = feedback.OverallComment;
                    response.SuggestedAnswer = feedback.SuggestedAnswer;
                    response.BloomScore = feedback.BloomScore;
                    response.DemonstratedBloomLevel = feedback.DemonstratedBloomLevel;
                    response.TechnicalDepthScore = feedback.TechnicalDepthScore;
                    response.ProblemSolvingScore = feedback.ProblemSolvingScore;
                    response.CommunicationScore = feedback.CommunicationScore;
                    response.PracticalExperienceScore = feedback.PracticalExperienceScore;
                    response.StructuredFeedbackJson = rawFeedback;

                    await _dataProvider.UpdateResponseAsync(response);

                    var avgScore = new[] { feedback.TechnicalDepthScore, feedback.ProblemSolvingScore, feedback.CommunicationScore, feedback.PracticalExperienceScore }
                        .Where(s => s.HasValue).Select(s => s!.Value).DefaultIfEmpty(0).Average();
                    totalScores.Add(avgScore);

                    _logger.LogInformation("Feedback generated for Response {ResponseId}, Turn {Turn}",
                        response.Id, response.TurnNumber);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating feedback for Response {ResponseId}: {Message}",
                        response.Id, ex.Message);
                }
            }

            var overallAvg = totalScores.Any() ? totalScores.Average() : 0.0;
            return $"Bạn đã hoàn thành {answeredResponses.Count} câu hỏi. " +
                $"Điểm trung bình tổng thể: {overallAvg:F2}/1.00. " +
                $"Hãy xem chi tiết feedback cho từng câu hỏi để cải thiện kỹ năng của bạn.";
        }

        // ── Private helpers ──

        private static string BuildQuestionUserPrompt(InterviewSessionData session, List<InterviewResponseData> previousResponses, double? estimatedAbility, List<QuestionBankItem>? ragQuestions)
        {
            var sb = new StringBuilder();

            int turnNumber = previousResponses.Count + 1;
            // Xác định đang ở Chunk nào
            string currentPhase = turnNumber switch
            {
                <= 2 => "Giai đoạn 1: Giới thiệu bản thân (Ice-breaker)",
                <= 4 => "Giai đoạn 2: Câu hỏi kỹ thuật chuyên môn (Technical)",
                <= 7 => "Giai đoạn 3: Tình huống giả định (Situational)",
                8 => "Giai đoạn 4: Đào sâu tình huống từ câu trả lời trước (Deep-dive)",
                _ => "Giai đoạn 5: Văn hóa làm việc và mức độ phù hợp (Culture fit)"
            };

            sb.AppendLine("=== THÔNG TIN PHIÊN PHỎNG VẤN ===");
            if (!string.IsNullOrEmpty(session.PositionName)) sb.AppendLine($"Vị trí: {session.PositionName}");
            if (!string.IsNullOrEmpty(session.SkillName)) sb.AppendLine($"Kỹ năng: {session.SkillName}");
            if (!string.IsNullOrEmpty(session.LevelName)) sb.AppendLine($"Cấp độ: {session.LevelName}");
            if (!string.IsNullOrEmpty(session.CompanyName)) sb.AppendLine($"Công ty: {session.CompanyName}");
            sb.AppendLine($"\nCâu hỏi thứ: {previousResponses.Count + 1}/{MaxQuestionsPerSession}");
            if (estimatedAbility.HasValue) sb.AppendLine($"Năng lực ước tính: {estimatedAbility.Value:F2}");

            sb.AppendLine($"\n**TRẠNG THÁI HIỆN TẠI: Đang ở Câu hỏi thứ {turnNumber}/{MaxQuestionsPerSession}**");
            sb.AppendLine($"**>>> YÊU CẦU: HÃY ĐẶT 1 CÂU HỎI THUỘC CHỦ ĐỀ CỦA [{currentPhase}] <<<**\n");


            if (turnNumber >= 3 && turnNumber <= 4 && ragQuestions?.Count > 0)
            {
                sb.AppendLine("\n=== NGÂN HÀNG CÂU HỎI THAM KHẢO TỪ DATABASE ===");
                sb.AppendLine("Hãy sử dụng các câu hỏi mẫu sau làm chất liệu tham khảo để biến tấu ra câu hỏi cho ứng viên sao cho phù hợp với cấp độ và JD (Không copy nguyên văn):");
                foreach (var q in ragQuestions)
                {
                    sb.AppendLine($"- Tham khảo: {q.Content}");
                }
            }

            // Thêm nội dung CV ứng viên để cá nhân hóa câu hỏi
            if (!string.IsNullOrEmpty(session.CvContent))
            {
                sb.AppendLine("\n=== THÔNG TIN CV ỨNG VIÊN ===");
                sb.AppendLine(session.CvContent.Length > 2000
                    ? session.CvContent.Substring(0, 2000) + "..."
                    : session.CvContent);
                sb.AppendLine("=== HẾT CV ===");
            }

            // Thêm JD gốc để câu hỏi sát yêu cầu công việc
            if (!string.IsNullOrEmpty(session.JobDescriptionText))
            {
                sb.AppendLine("\n=== MÔ TẢ CÔNG VIỆC (JD) ===");
                sb.AppendLine(session.JobDescriptionText.Length > 1500
                    ? session.JobDescriptionText.Substring(0, 1500) + "..."
                    : session.JobDescriptionText);
                sb.AppendLine("=== HẾT JD ===");
            }

            if (previousResponses.Any())
            {
                sb.AppendLine("\n=== LỊCH SỬ CÂU HỎI TRƯỚC ===");
                foreach (var r in previousResponses.TakeLast(5))
                {
                    sb.AppendLine($"\nCâu {r.TurnNumber}:");
                    sb.AppendLine($"  Hỏi: {r.QuestionContent}");
                    if (!string.IsNullOrEmpty(r.UserAnswer)) sb.AppendLine($"  Trả lời: {r.UserAnswer}");
                    if (r.DifficultyScore.HasValue) sb.AppendLine($"  Độ khó: {r.DifficultyScore.Value:F2}");
                }
                sb.AppendLine("=== HẾT LỊCH SỬ ===");
            }

            sb.AppendLine("\nDựa vào context trên (bao gồm CV và JD nếu có), hãy tạo câu hỏi phỏng vấn tiếp theo.");
            sb.AppendLine("Ưu tiên hỏi về kinh nghiệm thực tế trong CV kết hợp yêu cầu trong JD. KHÔNG lặp lại chủ đề câu trước.");
            if (previousResponses.Count == 0)
                sb.AppendLine("Đây là câu hỏi đầu tiên, hãy bắt đầu với độ khó vừa phải.");

            return sb.ToString();
        }

        private static string BuildFeedbackUserPrompt(InterviewResponseData response)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ĐÁNH GIÁ CÂU TRẢ LỜI ===");
            sb.AppendLine($"Câu hỏi: {response.QuestionContent}");
            sb.AppendLine($"Câu trả lời: {response.UserAnswer}");
            if (!string.IsNullOrEmpty(response.ExpectedAnswerOutline))
                sb.AppendLine($"Gợi ý đáp án: {response.ExpectedAnswerOutline}");
            if (response.ExpectedBloomLevel.HasValue)
                sb.AppendLine($"Bloom Level mong đợi: {response.ExpectedBloomLevel}");
            sb.AppendLine("\nHãy đánh giá câu trả lời trên và trả về JSON feedback.");
            return sb.ToString();
        }

        private GenerateQuestionResult ParseQuestionResponse(string rawResponse)
        {
            var cleaned = CleanJsonResponse(rawResponse);
            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;
                var result = new GenerateQuestionResult
                {
                    QuestionText = root.GetProperty("questionText").GetString() ?? "",
                    ExpectedAnswerOutline = root.TryGetProperty("expectedAnswerOutline", out var outline) ? outline.GetString() : null,
                    Topic = root.TryGetProperty("topic", out var topic) ? topic.GetString() : null,
                    Metrics = new QuestionMetrics()
                };

                if (root.TryGetProperty("bloomLevel", out var bloom))
                    result.Metrics.BloomTaxonomy = new BloomInfo
                    {
                        Level = bloom.GetInt32(),
                        LevelName = root.TryGetProperty("bloomLevelName", out var bName) ? bName.GetString() ?? "" : ""
                    };
                if (root.TryGetProperty("difficultyScore", out var diff))
                    result.Metrics.Irt = new IrtInfo { DifficultyScore = diff.GetDouble() };
                if (root.TryGetProperty("cognitiveLoad", out var clt))
                    result.Metrics.Clt = new CltInfo { TotalCognitiveLoad = clt.GetDouble() };
                if (root.TryGetProperty("questionType", out var qType))
                    result.Metrics.QuestionType = qType.GetString();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse question response");
                return new GenerateQuestionResult { QuestionText = cleaned, Topic = "general" };
            }
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

        public async Task<SetupInterviewResult> ClassifyJobDescriptionAsync(string jobDescriptionText)
        {
            var systemPrompt = @"Bạn là chuyên gia phân tích tuyển dụng IT. Nhiệm vụ của bạn là phân tích Job Description (JD) và trích xuất thông tin chính.
Trả về JSON với format chính xác sau (KHÔNG markdown, KHÔNG giải thích):
{
  ""position"": ""Tên vị trí công việc"",
  ""skill"": ""Kỹ năng chính"",
  ""skills"": [""skill1"", ""skill2"", ""skill3""],
  ""level"": ""Junior/Middle/Senior/Lead/Manager"",
  ""company"": ""Tên công ty (null nếu không có)"",
  ""requirements"": [""Yêu cầu 1"", ""Yêu cầu 2""],
  ""levelMismatchWarning"": null
}
Lưu ý:
- position: Xác định vị trí chính xác nhất, ví dụ: Backend Developer, Frontend Engineer, DevOps Engineer
- skills: Liệt kê 3-7 kỹ năng kỹ thuật chính
- level: Phán đoán từ yêu cầu kinh nghiệm (0-1 năm: Junior, 2-4: Middle, 5+: Senior)
- requirements: Tóm tắt 3-5 yêu cầu chính
- Nếu JD quá ngắn hoặc không rõ ràng, vẫn cố gắng phân loại hợp lý nhất";

            var userPrompt = $"Hãy phân tích JD sau:\n\n{jobDescriptionText}";

            _logger.LogInformation("Classifying JD ({Length} chars)", jobDescriptionText.Length);
            var rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);

            return ParseSetupResponse(rawResponse);
        }

        public async Task<string> GenerateReactionAsync(int sessionId, string question, string userAnswer)
        {
            var systemPrompt = @"Bạn là phỏng vấn viên AI tên Bernie, chuyên phỏng vấn IT. 
Sau khi ứng viên trả lời, hãy phản hồi ngắn gọn (1-2 câu) một cách tự nhiên và chuyên nghiệp.

QUY TẮC:
- Nhận xét tích cực nếu câu trả lời tốt (ví dụ: 'Câu trả lời rất chi tiết!', 'Tốt lắm!')
- Gợi ý nhẹ nếu câu trả lời chung chung (ví dụ: 'Bạn có thể cho ví dụ cụ thể hơn không?')
- Chuyển tiếp tự nhiên sang câu hỏi tiếp theo (ví dụ: 'Hay lắm! Tiếp theo tôi muốn hỏi về...')
- KHÔNG đánh giá điểm, KHÔNG nói đáp án đúng/sai
- KHÔNG hỏi câu hỏi mới trong phản hồi
- Trả về text thuần, KHÔNG markdown, KHÔNG JSON";

            var userPrompt = $"Câu hỏi phỏng vấn: {question}\nCâu trả lời của ứng viên: {userAnswer}\n\nHãy phản hồi ngắn gọn (1-2 câu):";

            _logger.LogInformation("Generating AI reaction for session {SessionId}", sessionId);
            var reaction = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);
            return reaction.Trim();
        }

        private SetupInterviewResult ParseSetupResponse(string rawResponse)
        {
            var cleaned = CleanJsonResponse(rawResponse);
            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;

                var skills = root.TryGetProperty("skills", out var skillsArr)
                    ? skillsArr.EnumerateArray().Select(s => s.GetString() ?? "").ToArray()
                    : Array.Empty<string>();

                var requirements = root.TryGetProperty("requirements", out var reqArr)
                    ? reqArr.EnumerateArray().Select(s => s.GetString() ?? "").ToArray()
                    : null;

                return new SetupInterviewResult
                {
                    Position = root.TryGetProperty("position", out var pos) ? pos.GetString() ?? "Software Developer" : "Software Developer",
                    Skill = root.TryGetProperty("skill", out var skill) ? skill.GetString() ?? (skills.FirstOrDefault() ?? "") : (skills.FirstOrDefault() ?? ""),
                    Skills = skills,
                    Level = root.TryGetProperty("level", out var level) ? level.GetString() ?? "Junior" : "Junior",
                    Company = root.TryGetProperty("company", out var company) && company.ValueKind != JsonValueKind.Null ? company.GetString() : null,
                    Requirements = requirements,
                    LevelMismatchWarning = root.TryGetProperty("levelMismatchWarning", out var warn) && warn.ValueKind != JsonValueKind.Null ? warn.GetString() : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse setup response");
                return new SetupInterviewResult
                {
                    Position = "Software Developer",
                    Skill = "General",
                    Skills = new[] { "General" },
                    Level = "Junior"
                };
            }
        }

        private static string CleanJsonResponse(string text)
        {
            var cleaned = Regex.Replace(text.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "");
            return cleaned.Trim();
        }

        private async Task<List<QuestionBankItem>> GetReferenceQuestionsAsync(string field, string level)
        {
            try
            {
                // Lấy 5 câu hỏi ngẫu nhiên trong DB đúng chuyên môn làm dữ liệu mồi (RAG)
                if (_questionDataProvider != null)
                {
                    return await _questionDataProvider.GetQuestionsAsync(field, level, 5);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi lấy câu hỏi mồi từ Database");
            }
            return new List<QuestionBankItem>();
        }
    }
}
