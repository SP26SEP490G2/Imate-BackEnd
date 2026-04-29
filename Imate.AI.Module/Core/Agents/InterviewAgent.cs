using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Agents
{
    /// <summary>
    /// Agent phỏng vấn AI (Tầng 3 - Agents)
    /// Chịu trách nhiệm: build prompt, gọi AI Service, parse response
    /// Không truy cập data layer trực tiếp — nhận dữ liệu từ Orchestrator
    /// </summary>
    public class InterviewAgent : IInterviewAgent
    {
        private readonly IGeminiService _geminiService;
        private readonly IQuestionDataProvider? _questionDataProvider;
        private readonly ILogger<InterviewAgent> _logger;

        private const int MaxQuestionsPerSession = 10;
        private static readonly string _questionSystemPrompt;

        static InterviewAgent()
        {
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var questionPromptPath = Path.Combine(basePath, "SystemMessages", "interview-question-system.txt");

            _questionSystemPrompt = File.Exists(questionPromptPath)
                ? File.ReadAllText(questionPromptPath)
                : "Bạn là chuyên gia phỏng vấn IT. Tạo câu hỏi phỏng vấn và trả về JSON.";
        }

        public InterviewAgent(
            IGeminiService geminiService,
            ILogger<InterviewAgent> logger,
            IQuestionDataProvider? questionDataProvider = null)
        {
            _geminiService = geminiService;
            _logger = logger;
            _questionDataProvider = questionDataProvider;
        }

        public async Task<string> GenerateWelcomeMessageAsync(string? cvContent, string? positionName, string? companyName, string? language = null)
        {
            var lang = language ?? "vi-VN";
            var systemPrompt = "Bạn là phỏng vấn viên AI tên imAI, chuyên phỏng vấn IT. Hãy tạo lời chào mừng ngắn gọn, thân thiện, chuyên nghiệp cho buổi phỏng vấn. Trả về text thuần, KHÔNG trả JSON.";

            var sb = new StringBuilder();
            sb.AppendLine("Hãy tạo lời chào mừng cho buổi phỏng vấn với thông tin:");
            if (!string.IsNullOrEmpty(positionName)) sb.AppendLine($"- Vị trí: {positionName}");
            if (!string.IsNullOrEmpty(companyName)) sb.AppendLine($"- Công ty: {companyName}");
            if (!string.IsNullOrEmpty(cvContent)) sb.AppendLine($"- Cv User: {cvContent}");
            sb.AppendLine($"- Ngôn ngữ: {(lang.StartsWith("vi") ? "Tiếng Việt" : "English")}");
            sb.AppendLine("Giới thiệu bản thân là imAI, giải thích ngắn gọn quy trình phỏng vấn. Tối đa 3-4 câu.");

            var welcomeMessage = await _geminiService.GenerateContentAsync(systemPrompt, sb.ToString());
            return welcomeMessage.Trim();
        }

        public async Task<GenerateQuestionResult> GenerateQuestionAsync(
            InterviewSessionData session,
            List<InterviewResponseData> existingResponses,
            double? estimatedAbility = null,
            List<string>? selectedGaps = null)
        {
            var turnNumber = existingResponses.Count + 1;
            var answeredCount = existingResponses.Count(r => !string.IsNullOrEmpty(r.UserAnswer));

            // Kiểm tra giới hạn câu hỏi
            if (answeredCount >= MaxQuestionsPerSession)
            {
                return new GenerateQuestionResult
                {
                    IsTerminated = true,
                    TerminationReason = "MaxQuestionsReached",
                    TerminationMessage = $"Buổi phỏng vấn đã hoàn thành {MaxQuestionsPerSession} câu hỏi. Cảm ơn bạn đã tham gia! Hệ thống đang tạo báo cáo phản hồi..."
                };
            }

            // Lấy câu hỏi tham khảo từ DB theo chunk hiện tại
            // Ưu tiên dùng selectedGaps nếu có (từ Training Journey), nếu không dùng SkillName
            var ragQuestions = await GetRagQuestionsForChunkAsync(
                turnNumber,
                selectedGaps,
                session.SkillName ?? "General",
                session.LevelName ?? "Junior");

            var userPrompt = BuildQuestionUserPrompt(session, existingResponses, estimatedAbility, ragQuestions);

            // Xác định chunk hiện tại cho log
            string chunkName = turnNumber switch
            {
                <= 2 => "CHUNK 1 - Ice-breaker",
                <= 4 => "CHUNK 2 - Technical",
                <= 7 => "CHUNK 3 - Situational",
                <= 9 => "CHUNK 4 - Deep-dive",
                _ => "CHUNK 5 - Culture"
            };

            _logger.LogInformation(
                "\n========== [INTERVIEW] GENERATING QUESTION ==========\n" +
                "  Session: {SessionId}\n" +
                "  Câu hỏi: {Turn}/{Max}\n" +
                "  Giai đoạn: {Chunk}\n" +
                "  RAG từ DB: {RagCount} câu tham khảo\n" +
                "=====================================================",
                session.Id, turnNumber, MaxQuestionsPerSession, chunkName, ragQuestions?.Count ?? 0);

            var delay = Random.Shared.Next(800, 1500);
            await Task.Delay(delay);
            var rawResponse = await _geminiService.GenerateContentAsync(_questionSystemPrompt, userPrompt);
            var questionData = ParseQuestionResponse(rawResponse);

            return questionData;
        }


        public async Task<SetupInterviewResult> ClassifyJobDescriptionAsync(string jobDescriptionText, string? cvText = null)
        {
            var systemPrompt = @"Bạn là chuyên gia phân tích tuyển dụng IT. Nhiệm vụ của bạn là phân tích Job Description (JD) và CV (nếu có) để trích xuất thông tin chính.
Trả về JSON với format chính xác sau (KHÔNG markdown, KHÔNG giải thích):
{
  ""position"": ""Tên vị trí công việc"",
  ""skill"": ""Kỹ năng chính"",
  ""skills"": [""skill1"", ""skill2"", ""skill3""],
  ""level"": ""Intern/Fresher/Junior/Middle/Senior/Lead/Manager"",
  ""company"": ""Tên công ty (null nếu không có)"",
  ""requirements"": [""Yêu cầu 1"", ""Yêu cầu 2""],
  ""levelMismatchWarning"": null,
  ""isItRelatedJd"": true,
  ""isItRelatedCv"": true,
  ""cvEstimatedLevel"": ""Junior""
}
Lưu ý:
- position: Xác định vị trí chính xác nhất, ví dụ: Backend Developer, Frontend Engineer, DevOps Engineer
- skills: Liệt kê 3-7 kỹ năng kỹ thuật chính
- level: Phán đoán cấp bậc yêu cầu trong JD:
  + Thực tập sinh / Intern / Trainee / Sinh viên: ""Intern""
  + Fresher / Dưới 1 năm kinh nghiệm: ""Fresher""
  + 1-2 năm kinh nghiệm: ""Junior""
  + 2-4 năm kinh nghiệm: ""Middle""
  + 5+ năm kinh nghiệm: ""Senior""
  + 8+ năm có kinh nghiệm quản lý nhóm: ""Lead""
  + Quản lý cấp cao / Director: ""Manager""
- requirements: Tóm tắt 3-5 yêu cầu chính
- isItRelatedJd: JD có thuộc ngành CNTT/Công nghệ thông tin/IT/Phần mềm không?
  + Nếu JD tuyển lập trình viên, kỹ sư phần mềm, DevOps, QA, BA, Data, AI/ML, Designer UI/UX → true
  + Nếu JD tuyển Bác sĩ, Luật sư, Kế toán, Giáo viên, Kiến trúc sư xây dựng, Y tá, Dược sĩ v.v. → false
- isItRelatedCv: Phân tích CV xem ứng viên có thuộc ngành CNTT/Công nghệ thông tin/IT không. 
  + Nếu CV chứa các kỹ năng lập trình, phần mềm, hệ thống, mạng, data, AI/ML, DevOps, QA, BA trong IT → true
  + Nếu CV thuộc ngành Y tế, Luật, Kế toán, Marketing thuần, Xây dựng, Cơ khí v.v. → false
  + Nếu không có CV (null) → true (mặc định)
- cvEstimatedLevel: Ước tính level kinh nghiệm trong CV dựa trên số năm kinh nghiệm và dự án thực tế:
  + 0 năm hoặc sinh viên: ""Intern""
  + Dưới 1 năm: ""Fresher""  
  + 1-2 năm: ""Junior""
  + 2-4 năm: ""Middle""
  + 5+ năm: ""Senior""
  + 8+ năm có kinh nghiệm quản lý: ""Lead""
  + Nếu không có CV → null
- Nếu JD quá ngắn hoặc không rõ ràng, vẫn cố gắng phân loại hợp lý nhất";

            var userPrompt = new StringBuilder();
            userPrompt.AppendLine("Hãy phân tích JD sau:");
            userPrompt.AppendLine(jobDescriptionText);

            if (!string.IsNullOrWhiteSpace(cvText))
            {
                userPrompt.AppendLine("\n=== CV ỨNG VIÊN ===");
                userPrompt.AppendLine(cvText.Length > 3000 ? cvText.Substring(0, 3000) + "..." : cvText);
                userPrompt.AppendLine("=== HẾT CV ===");
            }

            _logger.LogInformation("Classifying JD ({JdLength} chars) + CV ({CvLength} chars)",
                jobDescriptionText.Length, cvText?.Length ?? 0);
            var rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt.ToString());

            return ParseSetupResponse(rawResponse);
        }

        public async Task<string> GenerateReactionAsync(string? gapAnalysisJson, string question, string userAnswer)
        {
            var systemPrompt = @"Bạn là một Mentor (người hướng dẫn) và Senior Interviewer dày dạn kinh nghiệm. 
Nhiệm vụ: Phản hồi lại câu trả lời của ứng viên một cách tự nhiên, mang tính khích lệ và dẫn dắt (Coaching).

QUY TẮC PHẢN HỒI:
1. PHONG CÁCH MENTOR: Phản hồi chuyên nghiệp, thân thiện. Hãy coi ứng viên như một đồng nghiệp tiềm năng cần được chỉ dẫn.
2. TẬP TRUNG VÀO NÂNG CẤP: Nếu ứng viên trả lời tốt, hãy khen ngợi cụ thể. Nếu chưa tốt, hãy đưa ra gợi ý nhẹ nhàng để họ tư duy sâu hơn.
3. LIÊN KẾT VỚI GAP (NẾU CÓ): Sử dụng thông tin GAP ANALYSIS để biết ứng viên đang thiếu gì so với JD. Nếu câu trả lời thể hiện nỗ lực lấp đầy Gap, hãy ghi nhận.
4. ĐỘ DÀI: Tối đa 1-2 câu ngắn gọn để không làm gián đoạn luồng phỏng vấn.
5. CHUYỂN TIẾP: Kết thúc bằng một câu dẫn để chuẩn bị cho câu hỏi tiếp theo.
6. CẤM: KHÔNG đánh giá điểm số, KHÔNG được hỏi câu hỏi mới ở đây, KHÔNG trả về JSON/Markdown.";

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(gapAnalysisJson))
            {
                sb.AppendLine("=== KHOẢNG TRỐNG NĂNG LỰC (GAP ANALYSIS) ===");
                sb.AppendLine(gapAnalysisJson);
                sb.AppendLine("=== HẾT GAP ANALYSIS ===\n");
            }
            sb.AppendLine($"Câu hỏi bạn vừa hỏi: {question}");
            sb.AppendLine($"Câu trả lời của ứng viên: {userAnswer}");
            sb.AppendLine("\nHãy đưa ra phản hồi mang tính Mentor (1-2 câu):");

            _logger.LogInformation("Generating Mentor reaction");
            var reaction = await _geminiService.GenerateContentAsync(systemPrompt, sb.ToString());
            return reaction.Trim();
        }

        public async Task<string> AnalyzeGapsAsync(string cvContent, string jobDescriptionText)
        {
            var systemPrompt = @"Bạn là chuyên gia phân tích nhân sự và đào tạo IT. 
Nhiệm vụ: So sánh CV ứng viên với JD (Mô tả công việc) để tìm ra các khoảng trống năng lực (Gaps).

PHÂN LOẠI GAPS - QUY TẮC RÕNG:

1. 'hardSkillsGaps': KỸ NĂNG CÓ THỂ LUYỆN TẬP qua phỏng vấn/training ngắn
   VD: Java, Spring, REST APIs, Docker, Kubernetes, AWS/Azure, databases, design patterns
   
2. 'softSkillsGaps': KỸ NĂNG MỀM CÓ THỂ LUYỆN TẬP qua phỏng vấn/training ngắn
   VD: giao tiếp kỹ thuật, code review skills, thiết kế hệ thống, phân tích vấn đề, mentoring skills
   QUAN TRỌNG: ""Mentoring skills"" là kỹ năng → softSkillsGap (có thể luyện qua phỏng vấn)
   
3. 'profileGaps': KINH NGHIỆM/BACKGROUND/BẰNG CẤP KHÔNG THỂ LUYỆN TẬP TRONG NGẮN HẠN:
   - Kinh nghiệm chuyên nghiệp: JD yêu cầu 5 năm, CV chỉ 1 năm → profileGap
   - Kinh nghiệm lĩnh vực: JD cần Banking/Payment, CV chưa từng làm → profileGap
   - Kinh nghiệm quản lý: JD yêu cầu ""quản lý team dev chuyên nghiệp"", CV chỉ quản lý CLB/sự kiện → profileGap (experience-based, need real practice)
   - Bằng cấp: JD yêu cầu đại học, CV chỉ có cao đẳng → profileGap
   - Background thực tế: cần đã từng làm dự án thực tế → profileGap

PHÂN BIỆT QUAN TRỌNG:
- ""Kỹ năng quản lý"" (Management Skills) → softSkillsGap (luyện tập được)
- ""Kinh nghiệm quản lý team development"" (Management Experience) → profileGap (cần hành động thực tế)

QUYẾT ĐỊNH:
1. Gap có từ ""kinh nghiệm"", ""chưa từng"", ""không có kinh nghiệm"", ""background"", ""thực tế"" + cần hành động thực tế → profileGap
2. Gap về KỸ NĂNG (Java, Spring, design, communication, leadership, mentoring) → hardSkill/softSkill
3. Nếu không chắc: Có thể luyện tập/cải thiện trong 1-2 buổi phỏng vấn? → hardSkill/softSkill; Không → profileGap

Trả về JSON (LUÔN có cả 5 fields):
{
  ""hardSkillsGaps"": [""Java EE"", ""REST API design""],
  ""softSkillsGaps"": [""Code review skills"", ""Mentoring skills""],
  ""profileGaps"": [
    ""Kinh nghiệm thực tế: JD yêu cầu 5 năm, CV hiện có 1 năm"",
    ""Kinh nghiệm quản lý team development: CV chỉ quản lý CLB/sự kiện, chưa quản lý team dev chuyên nghiệp""
  ],
  ""suitabilityStrengths"": [""Hiểu rõ OOP"", ""Database design""],
  ""trainingFocus"": ""Tập trung huấn luyện Java EE, REST APIs, code review skills, mentoring skills...""
}";
            var userPrompt = $"=== CV CHI TIẾT ===\n{cvContent}\n\n=== JD YÊU CẦU ===\n{jobDescriptionText}\n\nPhân tích Gaps (PHẢI trả về profileGaps dù có thể rỗng):";

            _logger.LogInformation("[GAP-ANALYSIS] Bắt đầu phân tích Gap CV vs JD...");
            var result = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);

            // Debug: log response
            _logger.LogInformation("[GAP-ANALYSIS] Response từ Gemini: {Response}", result);

            return CleanJsonResponse(result);
        }

        // ── Private helpers ──

        private static string BuildQuestionUserPrompt(InterviewSessionData session, List<InterviewResponseData> previousResponses, double? estimatedAbility, List<QuestionBankItem>? ragQuestions)
        {
            var sb = new StringBuilder();

            int turnNumber = previousResponses.Count + 1;
            string currentPhase = turnNumber switch
            {
                <= 2 => "Giai đoạn 1: Giới thiệu bản thân (Ice-breaker)",
                <= 4 => "Giai đoạn 2: Câu hỏi kỹ thuật chuyên môn (Technical)",
                <= 7 => "Giai đoạn 3: Tình huống giả định (Situational)",
                <= 9 => "Giai đoạn 4: Đào sâu tình huống từ câu trả lời trước (Deep-dive)",
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

            // Inject câu hỏi tham khảo từ DB (nếu có)
            if (ragQuestions?.Count > 0)
            {
                sb.AppendLine("\n=== NGÂN HÀNG CÂU HỎI THAM KHẢO TỪ DATABASE ===");
                sb.AppendLine("Hãy sử dụng các câu hỏi mẫu sau làm chất liệu tham khảo để biến tấu ra câu hỏi cho ứng viên sao cho phù hợp với cấp độ và JD.");
                sb.AppendLine("KHÔNG copy nguyên văn — hãy paraphrase, thay đổi ngữ cảnh, hoặc kết hợp nhiều câu hỏi.");
                foreach (var q in ragQuestions)
                {
                    sb.AppendLine($"- Tham khảo: {q.Content}");
                    if (!string.IsNullOrWhiteSpace(q.SampleAnswer))
                        sb.AppendLine($"  Đáp án mẫu: {q.SampleAnswer}");
                }
                sb.AppendLine("=== HẾT THAM KHẢO ===");
            }
            else
            {
                sb.AppendLine("\n(Không có câu hỏi tham khảo từ DB — hãy TỰ sáng tạo câu hỏi dựa trên CV, JD và giai đoạn hiện tại.)");
            }

            if (!string.IsNullOrEmpty(session.CvContent))
            {
                sb.AppendLine("\n=== THÔNG TIN CV ỨNG VIÊN ===");
                var cvContent = session.CvContent.Length > 2000
                    ? session.CvContent.Substring(0, 2000) + "..."
                    : session.CvContent;
                sb.AppendLine(SanitizeForPrompt(cvContent));
                sb.AppendLine("=== HẾT CV ===");
            }

            // Thêm JD gốc để câu hỏi sát yêu cầu công việc (sanitized để tránh prompt injection)
            if (!string.IsNullOrEmpty(session.JobDescriptionText))
            {
                sb.AppendLine("\n=== MÔ TẢ CÔNG VIỆC (JD) ===");
                var jdContent = session.JobDescriptionText.Length > 1500
                    ? session.JobDescriptionText.Substring(0, 1500) + "..."
                    : session.JobDescriptionText;
                sb.AppendLine(SanitizeForPrompt(jdContent));
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
            sb.AppendLine("QUAN TRỌNG: CHỈ trả về JSON câu hỏi. KHÔNG viết lời cảm ơn, nhận xét, hay phản hồi câu trả lời trước. Đi thẳng vào câu hỏi.");
            if (previousResponses.Count == 0)
                sb.AppendLine("Đây là câu hỏi đầu tiên, hãy bắt đầu với độ khó vừa phải. LƯU Ý: Lời chào đã được gửi riêng, KHÔNG chào lại. Đi thẳng vào câu hỏi.");
            Console.WriteLine(sb); 
            return sb.ToString();
        }

        private GenerateQuestionResult ParseQuestionResponse(string rawResponse)
        {
            var cleaned = CleanJsonResponse(rawResponse);

            // Validate: Response không được phải plain text (phải là JSON)
            if (!cleaned.StartsWith("{") || !cleaned.EndsWith("}"))
            {
                _logger.LogError(
                    "[QUESTION-PARSE] Response không phải JSON object. Response bắt đầu với: {Start}",
                    cleaned.Substring(0, Math.Min(100, cleaned.Length)));
                return new GenerateQuestionResult
                {
                    QuestionText = "Lỗi: Hệ thống không thể generate câu hỏi. Vui lòng thử lại.",
                    Topic = "general",
                    IsValid = false
                };
            }

            try
            {
                using var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;

                // Validate: phải có questionText
                if (!root.TryGetProperty("questionText", out var questionProp))
                {
                    _logger.LogError("[QUESTION-PARSE] JSON không có field 'questionText'. JSON: {Json}",
                        cleaned.Substring(0, Math.Min(200, cleaned.Length)));
                    return new GenerateQuestionResult
                    {
                        QuestionText = "Lỗi: Câu hỏi không hợp lệ (thiếu questionText field).",
                        Topic = "general",
                        IsValid = false
                    };
                }

                var questionText = questionProp.GetString()?.Trim();
                if (string.IsNullOrEmpty(questionText))
                {
                    _logger.LogError("[QUESTION-PARSE] questionText field rỗng");
                    return new GenerateQuestionResult
                    {
                        QuestionText = "Lỗi: Câu hỏi rỗng.",
                        Topic = "general",
                        IsValid = false
                    };
                }

                var result = new GenerateQuestionResult
                {
                    QuestionText = questionText,
                    ExpectedAnswerOutline = root.TryGetProperty("expectedAnswerOutline", out var outline) ? outline.GetString() : null,
                    Topic = root.TryGetProperty("topic", out var topic) ? topic.GetString() : null,
                    Metrics = new QuestionMetrics(),
                    IsValid = true
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

                _logger.LogInformation("[QUESTION-PARSE] ✅ Parsed successfully: {Question:M100}", questionText);
                return result;
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex,
                    "[QUESTION-PARSE] JSON parse error. Response: {Response}",
                    cleaned.Substring(0, Math.Min(300, cleaned.Length)));
                return new GenerateQuestionResult
                {
                    QuestionText = "Lỗi: Dữ liệu câu hỏi không hợp lệ.",
                    Topic = "general",
                    IsValid = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[QUESTION-PARSE] Unexpected error parsing question response");
                return new GenerateQuestionResult
                {
                    QuestionText = "Lỗi: Không thể xử lý câu hỏi.",
                    Topic = "general",
                    IsValid = false
                };
            }
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
                    LevelMismatchWarning = root.TryGetProperty("levelMismatchWarning", out var warn) && warn.ValueKind != JsonValueKind.Null ? warn.GetString() : null,
                    IsItRelatedJd = root.TryGetProperty("isItRelatedJd", out var itJd) && itJd.ValueKind == JsonValueKind.False ? false : true,
                    IsItRelatedCv = root.TryGetProperty("isItRelatedCv", out var itCv) && itCv.ValueKind == JsonValueKind.False ? false : true,
                    CvEstimatedLevel = root.TryGetProperty("cvEstimatedLevel", out var cvLvl) && cvLvl.ValueKind != JsonValueKind.Null ? cvLvl.GetString() : null
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

        /// <summary>
        /// Sanitize user input (CV, JD, etc) để tránh prompt injection từ content chứa JSON/markdown/code blocks.
        /// </summary>
        private static string SanitizeForPrompt(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            var sanitized = input
                // Remove code blocks markers (tránh confuse AI về structure)
                .Replace("```json", "[CODE_BLOCK]")
                .Replace("```", "[CODE_BLOCK]")
                // Escape curly braces (JSON-like structures)
                .Replace("{", "(")
                .Replace("}", ")")
                // Remove excessive newlines
                .Replace("\r\n\r\n\r\n", "\r\n\r\n");

            return sanitized;
        }

        /// <summary>
        /// Lấy câu hỏi tham khảo từ DB theo chunk hiện tại.
        /// 60% xác suất dùng DB, 40% để AI tự gen → giảm trùng lặp giữa các phiên.
        /// </summary>
        private async Task<List<QuestionBankItem>> GetRagQuestionsForChunkAsync(int turnNumber, List<string>? selectedGaps, string field, string level)
        {
            // Nếu có selectedGaps (Training Journey): ưu tiên AI tự gen theo gaps
            // Không dùng DB vì DB chưa có structure để filter theo gaps
            if (selectedGaps?.Count > 0)
            {
                _logger.LogInformation(
                    "[RAG] Câu {Turn}: Training Journey mode - AI tự gen theo gaps ({Gaps}), skip DB",
                    turnNumber, string.Join(",", selectedGaps));
                return new List<QuestionBankItem>();
            }

            // 40% xác suất AI tự gen (không query DB) → đa dạng câu hỏi
            if (Random.Shared.NextDouble() < 0.4)
            {
                _logger.LogInformation("[RAG] Câu {Turn}: Chọn chế độ AI tự gen (40% random) để tránh trùng lặp", turnNumber);
                return new List<QuestionBankItem>();
            }


            try
            {
                if (_questionDataProvider == null)
                    return new List<QuestionBankItem>();

                int maxCount = turnNumber switch
                {
                    <= 2 => 0,   // Chunk 1: Ice-breaker — KHÔNG dùng DB
                    <= 4 => 5,   // Chunk 2: Tech — nhiều tham khảo
                    <= 7 => 4,   // Chunk 3: Tình huống
                    <= 9 => 0,   // Chunk 4: Deep-dive — dựa vào câu trước
                    _    => 2,   // Chunk 5: Văn hóa
                };

                if (maxCount == 0)
                {
                    _logger.LogInformation("[RAG] Câu {Turn}: Chunk không cần câu hỏi DB", turnNumber);
                    return new List<QuestionBankItem>();
                }

                var questions = await _questionDataProvider.GetQuestionsAsync(field, level, maxCount);
                _logger.LogInformation(
                    "[RAG] Câu {Turn}: Lấy được {Count}/{Max} câu hỏi từ DB (field={Field}, level={Level})",
                    turnNumber, questions.Count, maxCount, field, level);

                return questions;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[RAG] Câu {Turn}: Lỗi khi lấy câu hỏi từ DB, fallback AI tự gen", turnNumber);
                return new List<QuestionBankItem>();
            }
        }
    }
}