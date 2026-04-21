using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Orchestrators
{
    /// <summary>
    /// Orchestrator phỏng vấn AI (Tầng 2 - Orchestrators)
    /// Điều phối workflow: data access → Agents → TTS → background tasks
    /// </summary>
    public class InterviewOrchestrator : IInterviewOrchestrator
    {
        private readonly IInterviewAgent _interviewAgent;
        private readonly IFeedbackAgent _feedbackAgent;
        private readonly IInterviewSessionDataProvider _dataProvider;
        private readonly ICvDataProvider _cvDataProvider;
        private readonly IAzureSpeechSynthesisService _speechSynthesisService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<InterviewOrchestrator> _logger;

        private const int MaxSessionDurationMinutes = 30;

        /// <summary>Level mapping for gap comparison</summary>
        private static readonly Dictionary<string, int> LevelOrder = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Intern"] = 0,
            ["Fresher"] = 1,
            ["Junior"] = 2,
            ["Middle"] = 3,
            ["Senior"] = 4,
            ["Lead"] = 5,
            ["Manager"] = 6
        };

        public InterviewOrchestrator(
            IInterviewAgent interviewAgent,
            IFeedbackAgent feedbackAgent,
            IInterviewSessionDataProvider dataProvider,
            ICvDataProvider cvDataProvider,
            IAzureSpeechSynthesisService speechSynthesisService,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<InterviewOrchestrator> logger)
        {
            _interviewAgent = interviewAgent;
            _feedbackAgent = feedbackAgent;
            _dataProvider = dataProvider;
            _cvDataProvider = cvDataProvider;
            _speechSynthesisService = speechSynthesisService;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task<InterviewLimitStatus> CheckInterviewCostAsync(int accountId)
        {
            return await _dataProvider.GetInterviewLimitStatusAsync(accountId);
        }

        public async Task<SetupInterviewResult> SetupInterviewAsync(int accountId, string jobDescriptionText, int? cvId = null)
        {
            // 1. Lấy CV text nếu có cvId
            string? cvText = null;
            if (cvId.HasValue)
            {
                try
                {
                    cvText = await _cvDataProvider.GetCvTextAsync(accountId, cvId.Value);
                    _logger.LogInformation("[SETUP] Loaded CV text for account {AccountId}, cvId {CvId} ({Length} chars)",
                        accountId, cvId.Value, cvText?.Length ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SETUP] Không thể đọc CV {CvId}, bỏ qua validation CV", cvId.Value);
                }
            }

            // 2. Gọi AI phân loại JD + CV
            var result = await _interviewAgent.ClassifyJobDescriptionAsync(jobDescriptionText, cvText);

            // 3. Validate: JD không thuộc ngành IT
            if (!result.IsItRelatedJd)
            {
                throw new InvalidOperationException(
                    "Mô tả công việc (JD) không thuộc ngành Công nghệ thông tin (IT). " +
                    "Hệ thống IMATE chỉ hỗ trợ phỏng vấn cho các vị trí trong ngành IT. " +
                    "Vui lòng nhập JD cho vị trí IT (lập trình viên, kỹ sư phần mềm, DevOps, QA, BA, Data...).");
            }

            // 4. Validate: CV không thuộc ngành IT
            if (!string.IsNullOrEmpty(cvText) && !result.IsItRelatedCv)
            {
                throw new InvalidOperationException(
                    "CV của bạn không thuộc ngành Công nghệ thông tin (IT). " +
                    "Hệ thống IMATE chỉ hỗ trợ phỏng vấn cho các vị trí trong ngành IT. " +
                    "Vui lòng tải lên CV phù hợp với ngành IT.");
            }

            // 5. Validate: Chênh lệch level giữa CV và JD >= 2
            if (!string.IsNullOrEmpty(cvText) && !string.IsNullOrEmpty(result.CvEstimatedLevel))
            {
                var jdLevel = result.Level ?? "Junior";
                var cvLevel = result.CvEstimatedLevel;

                if (LevelOrder.TryGetValue(jdLevel, out var jdLevelIdx) &&
                    LevelOrder.TryGetValue(cvLevel, out var cvLevelIdx))
                {
                    var gap = Math.Abs(jdLevelIdx - cvLevelIdx);
                    _logger.LogInformation("[SETUP] Level comparison: CV={CvLevel}({CvIdx}) vs JD={JdLevel}({JdIdx}), Gap={Gap}",
                        cvLevel, cvLevelIdx, jdLevel, jdLevelIdx, gap);

                    if (gap >= 2)
                    {
                        throw new InvalidOperationException(
                            $"CV của bạn ở mức {cvLevel} nhưng JD yêu cầu cấp bậc {jdLevel} — " +
                            $"chênh lệch {gap} bậc. Vui lòng chọn JD phù hợp hơn với kinh nghiệm hiện tại của bạn, " +
                            $"hoặc cập nhật CV để phản ánh đúng năng lực.");
                    }
                }
            }

            return result;
        }

        public async Task<int> CreateSessionAsync(int accountId, CreateInterviewSessionRequest request)
        {
            // Kiểm tra giới hạn lượt phỏng vấn
            var limitStatus = await _dataProvider.GetInterviewLimitStatusAsync(accountId);
            if (!limitStatus.CanStart)
            {
                throw new InvalidOperationException(limitStatus.Message);
            }

            var session = new InterviewSessionData
            {
                AccountId = accountId,
                StartTime = DateTimeOffset.UtcNow,
                Status = "InProgress",
                InterviewType = "FullSession",
                PositionName = request.PositionName,
                SkillName = request.SkillName ?? (request.SkillNames != null ? string.Join(", ", request.SkillNames) : null),
                LevelName = request.LevelName,
                CompanyName = request.CompanyName,
                JobDescriptionText = request.JobDescriptionText,
                UserCvId = request.CvId,
                CvContent = request.CvContent
            };

            var sessionId = await _dataProvider.CreateSessionAsync(session);

            _logger.LogInformation("Interview session created: {SessionId} for account {AccountId}",
                sessionId, accountId);

            // Tăng số lượt đã sử dụng (cho gói trả phí)
            await _dataProvider.IncrementMockInterviewUsageAsync(accountId);

            return sessionId;
        }

        public async Task<WelcomeMessageResult> GetWelcomeMessageAsync(int accountId, int sessionId, CancellationToken cancellationToken)
        {
            var session = await _dataProvider.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên phỏng vấn {sessionId}");

            if (session.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên phỏng vấn này.");

            if (session.Status != "InProgress")
                throw new InvalidOperationException("Phiên phỏng vấn này đã kết thúc hoặc bị hủy.");

            var welcomeMessage = await _interviewAgent.GenerateWelcomeMessageAsync(
                session.CvContent, session.PositionName, session.CompanyName);

            string? audioBase64 = null;
            string? mimeType = null;
            try
            {
                var speechResult = await _speechSynthesisService.SynthesizeToBase64Async(
                    welcomeMessage, language: "vi-VN", cancellationToken: CancellationToken.None);
                audioBase64 = speechResult.AudioBase64;
                mimeType = speechResult.MimeType;
            }
            catch (Exception ttsEx)
            {
                _logger.LogWarning(ttsEx, "Lỗi khi gọi TTS cho lời chào");
            }

            return new WelcomeMessageResult
            {
                WelcomeMessage = welcomeMessage,
                AudioBase64 = audioBase64,
                MimeType = mimeType
            };
        }

        public async Task<GenerateQuestionResult> GenerateQuestionAsync(
            int accountId, int sessionId, double? estimatedAbility, CancellationToken cancellationToken)
        {
            var session = await _dataProvider.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên phỏng vấn {sessionId}");

            if (session.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên phỏng vấn này.");

            if (session.Status != "InProgress")
                throw new InvalidOperationException("Phiên phỏng vấn này đã kết thúc hoặc bị hủy.");

            // Kiểm tra giới hạn thời gian (30 phút)
            var elapsedTime = DateTimeOffset.UtcNow - session.StartTime;
            if (elapsedTime.TotalMinutes >= MaxSessionDurationMinutes)
            {
                return new GenerateQuestionResult
                {
                    IsTerminated = true,
                    TerminationReason = "TimeLimitReached",
                    TerminationMessage = $"Buổi phỏng vấn đã quá thời gian quy định ({MaxSessionDurationMinutes} phút). Cảm ơn bạn đã tham gia! Hệ thống đang tạo báo cáo phản hồi..."
                };
            }

            // Tự động phân tích Gap nếu chưa có
            if (string.IsNullOrEmpty(session.GapAnalysisJson) && !string.IsNullOrEmpty(session.CvContent) && !string.IsNullOrEmpty(session.JobDescriptionText))
            {
                try 
                {
                    session.GapAnalysisJson = await _interviewAgent.AnalyzeGapsAsync(session.CvContent, session.JobDescriptionText);
                    await _dataProvider.UpdateSessionAsync(session);
                    _logger.LogInformation("[GAP-ANALYSIS] Đã cập nhật kết quả phân tích Gap cho Session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GAP-ANALYSIS] Lỗi khi phân tích Gap cho Session {SessionId}", sessionId);
                }
            }

            var existingResponses = await _dataProvider.GetResponsesBySessionIdAsync(sessionId);

            // Gọi Agent tạo câu hỏi
            var result = await _interviewAgent.GenerateQuestionAsync(session, existingResponses, estimatedAbility);

            if (result.IsTerminated)
            {
                // TTS cho thông báo kết thúc
                try
                {
                    var speechTermResult = await _speechSynthesisService.SynthesizeToBase64Async(
                        result.TerminationMessage ?? "Buổi phỏng vấn kết thúc.",
                        language: "vi-VN", cancellationToken: CancellationToken.None);
                    result.AudioBase64 = speechTermResult.AudioBase64;
                    result.MimeType = speechTermResult.MimeType;
                }
                catch (Exception ttsEx)
                {
                    _logger.LogWarning(ttsEx, "Lỗi TTS thông báo kết thúc phỏng vấn");
                }
                return result;
            }

            // Lưu câu hỏi vào DB
            var newResponse = new InterviewResponseData
            {
                InterviewSessionId = sessionId,
                TurnNumber = existingResponses.Count + 1,
                QuestionContent = result.QuestionText,
                ExpectedAnswerOutline = result.ExpectedAnswerOutline,
                ExpectedBloomLevel = result.Metrics?.BloomTaxonomy?.Level,
                DifficultyScore = result.Metrics?.Irt?.DifficultyScore,
                CognitiveLoadScore = result.Metrics?.Clt?.TotalCognitiveLoad
            };

            var savedId = await _dataProvider.CreateResponseAsync(newResponse);
            result.InterviewResponseId = savedId;

            _logger.LogInformation("[INTERVIEW] Câu hỏi đã lưu: ResponseId={ResponseId}, Turn={Turn}",
                savedId, existingResponses.Count + 1);

            // TTS cho câu hỏi
            try
            {
                var speechResult = await _speechSynthesisService.SynthesizeToBase64Async(
                    result.QuestionText, language: "vi-VN", cancellationToken: CancellationToken.None);
                result.AudioBase64 = speechResult.AudioBase64;
                result.MimeType = speechResult.MimeType;
            }
            catch (Exception ttsEx)
            {
                _logger.LogWarning(ttsEx, "Lỗi TTS cho câu hỏi phỏng vấn");
            }

            return result;
        }

        public async Task<SubmitAnswerResult> SubmitAnswerAsync(
            int accountId, SubmitAnswerRequest request, CancellationToken cancellationToken)
        {
            var session = await _dataProvider.GetSessionByIdAsync(request.InterviewSessionId);
            if (session == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên phỏng vấn {request.InterviewSessionId}");

            if (session.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên này.");

            if (session.Status != "InProgress")
                throw new InvalidOperationException("Phiên phỏng vấn này đã kết thúc hoặc bị hủy.");

            var response = await _dataProvider.GetResponseByIdAsync(request.InterviewResponseId);
            if (response == null)
                throw new KeyNotFoundException($"Không tìm thấy câu hỏi {request.InterviewResponseId}");

            // Lưu câu trả lời
            response.UserAnswer = request.UserAnswer;
            response.AnswerTimestamp = DateTimeOffset.UtcNow;
            await _dataProvider.UpdateResponseAsync(response);

            session.TotalQuestionsAnswered += 1;
            await _dataProvider.UpdateSessionAsync(session);

            // Tạo phản hồi AI (mentor reaction) + TTS
            string? aiReaction = null;
            string? aiReactionAudioBase64 = null;
            string? mimeType = null;

            try
            {
                aiReaction = await _interviewAgent.GenerateReactionAsync(
                    session.GapAnalysisJson, response.QuestionContent, request.UserAnswer);

                if (!string.IsNullOrEmpty(aiReaction))
                {
                    var speechResult = await _speechSynthesisService.SynthesizeToBase64Async(
                        aiReaction, language: "vi-VN", cancellationToken: CancellationToken.None);
                    aiReactionAudioBase64 = speechResult.AudioBase64;
                    mimeType = speechResult.MimeType;
                }
            }
            catch (Exception reactionEx)
            {
                _logger.LogWarning(reactionEx, "Lỗi khi gọi dịch vụ tạo câu phản hồi hoặc TTS, bỏ qua.");
            }

            return new SubmitAnswerResult
            {
                AiReaction = aiReaction,
                AiReactionAudioBase64 = aiReactionAudioBase64,
                MimeType = mimeType
            };
        }

        public async Task EndInterviewAsync(int accountId, int sessionId)
        {
            var session = await _dataProvider.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên phỏng vấn {sessionId}");

            if (session.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên này.");

            if (session.Status == "Completed")
                return; // Đã hoàn thành rồi

            session.EndTime = DateTimeOffset.UtcNow;
            await _dataProvider.UpdateSessionAsync(session);

            // Chạy nền tạo feedback
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var feedbackAgent = scope.ServiceProvider.GetRequiredService<IFeedbackAgent>();
                    var dp = scope.ServiceProvider.GetRequiredService<IInterviewSessionDataProvider>();
                    var log = scope.ServiceProvider.GetRequiredService<ILogger<InterviewOrchestrator>>();

                    log.LogInformation("Background feedback started for session {SessionId}", sessionId);

                    var responses = await dp.GetResponsesBySessionIdAsync(sessionId);
                    var answeredResponses = responses
                        .Where(r => !string.IsNullOrEmpty(r.UserAnswer))
                        .OrderBy(r => r.TurnNumber)
                        .ToList();

                    var s = await dp.GetSessionByIdAsync(sessionId);
                    var gapAnalysis = s?.GapAnalysisJson;

                    log.LogInformation("[FEEDBACK] Bắt đầu tạo feedback song song cho {Count} câu hỏi...", answeredResponses.Count);

                    const int maxFeedbackRetries = 3;
                    const int feedbackRetryDelaySeconds = 15;

                    // Feedback song song từng câu
                    var feedbackTasks = answeredResponses.Select(async response =>
                    {
                        for (int attempt = 1; attempt <= maxFeedbackRetries; attempt++)
                        {
                            try
                            {
                                var feedback = await feedbackAgent.GeneratePerQuestionFeedbackAsync(response, gapAnalysis);

                                response.AIFeedback = feedback.OverallComment;
                                response.SuggestedAnswer = feedback.SuggestedAnswer;
                                response.BloomScore = feedback.BloomScore;
                                response.DemonstratedBloomLevel = feedback.DemonstratedBloomLevel;
                                response.TechnicalDepthScore = feedback.TechnicalDepthScore;
                                response.ProblemSolvingScore = feedback.ProblemSolvingScore;
                                response.CommunicationScore = feedback.CommunicationScore;
                                response.PracticalExperienceScore = feedback.PracticalExperienceScore;
                                response.StructuredFeedbackJson = System.Text.Json.JsonSerializer.Serialize(feedback);

                                await dp.UpdateResponseAsync(response);

                                var avg = new[] { feedback.TechnicalDepthScore, feedback.ProblemSolvingScore, feedback.CommunicationScore, feedback.PracticalExperienceScore }
                                    .Where(sc => sc.HasValue).Select(sc => sc!.Value).DefaultIfEmpty(0).Average();

                                log.LogInformation("[FEEDBACK] ✅ Câu {Turn} xong (attempt {Attempt})", response.TurnNumber, attempt);
                                return avg;
                            }
                            catch (Exception ex)
                            {
                                if (attempt < maxFeedbackRetries)
                                {
                                    log.LogWarning("[FEEDBACK] ⚠ Câu {Turn} lỗi (attempt {Attempt}): {Message}. Thử lại sau {Delay}s...",
                                        response.TurnNumber, attempt, ex.Message, feedbackRetryDelaySeconds);
                                    await Task.Delay(TimeSpan.FromSeconds(feedbackRetryDelaySeconds));
                                }
                                else
                                {
                                    log.LogError(ex, "[FEEDBACK] ❌ Câu {Turn} thất bại sau {Max} lần thử.", response.TurnNumber, maxFeedbackRetries);
                                }
                            }
                        }
                        return 0.0;
                    });

                    var scoresArray = await Task.WhenAll(feedbackTasks);
                    var totalScores = scoresArray.ToList();
                    log.LogInformation("[FEEDBACK] Hoàn thành feedback cho tất cả câu hỏi.");

                    var overallAvg = totalScores.Any() ? totalScores.Average() : 0.0;

                    // Tạo tổng kết
                    var overallFeedback = await feedbackAgent.GenerateSessionSummaryAsync(answeredResponses, overallAvg);

                    var sessionToUpdate = await dp.GetSessionByIdAsync(sessionId);
                    if (sessionToUpdate != null)
                    {
                        sessionToUpdate.Status = "Completed";
                        sessionToUpdate.OverallFeedback = overallFeedback;
                        await dp.UpdateSessionAsync(sessionToUpdate);
                    }

                    log.LogInformation("Background feedback completed for session {SessionId}", sessionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background feedback error for session {SessionId}", sessionId);
                }
            });
        }

        public async Task<InterviewResultData> GetInterviewResultAsync(int accountId, int sessionId)
        {
            var session = await _dataProvider.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên phỏng vấn {sessionId}");

            if (session.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên này.");

            var allResponses = await _dataProvider.GetResponsesBySessionIdAsync(sessionId);
            var answered = allResponses.Where(r => !string.IsNullOrEmpty(r.UserAnswer)).OrderBy(r => r.TurnNumber).ToList();
            var withFeedback = answered.Select((r, i) => new
            {
                id = r.Id, questionNumber = i + 1, turnNumber = r.TurnNumber,
                questionContent = r.QuestionContent, userAnswer = r.UserAnswer,
                answerTimestamp = r.AnswerTimestamp,
                expectedBloomLevel = r.ExpectedBloomLevel, demonstratedBloomLevel = r.DemonstratedBloomLevel,
                bloomScore = r.BloomScore, difficultyScore = r.DifficultyScore,
                cognitiveLoadScore = r.CognitiveLoadScore,
                technicalDepthScore = r.TechnicalDepthScore, problemSolvingScore = r.ProblemSolvingScore,
                communicationScore = r.CommunicationScore, practicalExperienceScore = r.PracticalExperienceScore,
                starSituationScore = r.StarSituationScore, starTaskScore = r.StarTaskScore,
                starActionScore = r.StarActionScore, starResultScore = r.StarResultScore,
                structuredFeedbackJson = r.StructuredFeedbackJson, aiFeedback = r.AIFeedback,
                expectedAnswerOutline = r.ExpectedAnswerOutline
            }).ToList();

            return new InterviewResultData
            {
                Session = new
                {
                    id = session.Id, positionName = session.PositionName, skillName = session.SkillName,
                    levelName = session.LevelName, companyName = session.CompanyName,
                    startTime = session.StartTime, endTime = session.EndTime,
                    status = session.Status, totalQuestions = answered.Count,
                    totalQuestionsAnswered = withFeedback.Count,
                    overallFeedback = session.OverallFeedback, estimatedAbility = session.EstimatedAbility
                },
                Responses = withFeedback
            };
        }

        public async Task<ResumeSessionData> ResumeSessionAsync(int accountId, int sessionId)
        {
            var session = await _dataProvider.GetSessionByIdAsync(sessionId);
            if (session == null)
                throw new KeyNotFoundException($"Không tìm thấy phiên phỏng vấn {sessionId}");

            if (session.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập phiên này.");

            var allResponses = await _dataProvider.GetResponsesBySessionIdAsync(sessionId);
            var orderedResponses = allResponses.OrderBy(r => r.TurnNumber).ToList();

            var lastUnanswered = orderedResponses.LastOrDefault(r => string.IsNullOrEmpty(r.UserAnswer));
            var answeredCount = orderedResponses.Count(r => !string.IsNullOrEmpty(r.UserAnswer));

            var responseList = orderedResponses.Select(r => new
            {
                id = r.Id,
                turnNumber = r.TurnNumber,
                questionContent = r.QuestionContent,
                userAnswer = r.UserAnswer,
                answerTimestamp = r.AnswerTimestamp,
            }).ToList();

            return new ResumeSessionData
            {
                Session = new
                {
                    id = session.Id,
                    positionName = session.PositionName,
                    skillName = session.SkillName,
                    levelName = session.LevelName,
                    companyName = session.CompanyName,
                    startTime = session.StartTime,
                    endTime = session.EndTime,
                    status = session.Status,
                },
                Responses = responseList,
                AnsweredCount = answeredCount,
                CurrentResponseId = lastUnanswered?.Id,
                HasUnansweredQuestion = lastUnanswered != null,
            };
        }

        public async Task<List<InterviewHistoryItem>> GetInterviewHistoryAsync(int accountId)
        {
            var sessions = await _dataProvider.GetSessionsByAccountIdAsync(accountId);
            return sessions.Select(s => new InterviewHistoryItem
            {
                Id = s.Id,
                PositionName = s.PositionName,
                SkillName = s.SkillName,
                LevelName = s.LevelName,
                CompanyName = s.CompanyName,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                TotalQuestionsAnswered = s.TotalQuestionsAnswered,
                EstimatedAbility = s.EstimatedAbility,
                Status = s.Status,
                InterviewType = s.QuestionId != null ? "Single_Question" : (s.UserCvId != null ? "CV_JD" : "Text"),
            }).ToList();
        }
    }
}