using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Claims;
using System.Text.Json;

namespace Imate.AI.Module.Controllers
{
    /// <summary>
    /// Controller phỏng vấn AI — UC-35: Practice Mock Interview
    /// Route: /api/ai-interview/*
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class AIInterviewController : ControllerBase
    {
        private readonly IInterviewService _interviewService;
        private readonly IInterviewSessionDataProvider _dataProvider;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ISpeechSynthesisService _speechSynthesisService;
        private readonly ILogger<AIInterviewController> _logger;

        public AIInterviewController(
            IInterviewService interviewService,
            IInterviewSessionDataProvider dataProvider,
            IServiceScopeFactory serviceScopeFactory,
            ISpeechSynthesisService speechSynthesisService,
            ILogger<AIInterviewController> logger)
        {
            _interviewService = interviewService;
            _dataProvider = dataProvider;
            _serviceScopeFactory = serviceScopeFactory;
            _speechSynthesisService = speechSynthesisService;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra chi phí phỏng vấn (lượt free / subscription)
        /// GET /api/ai-interview/check-interview-cost
        /// </summary>
        [HttpGet("ai-interview/check-interview-cost")]
        public IActionResult CheckInterviewCost()
        {
            // Hiện tại trả free mặc định — mở rộng subscription sau
            return Ok(new
            {
                success = true,
                data = new
                {
                    requiresPayment = false,
                    isFree = true,
                    freeUsedMock = 0,
                    freeLimit = 100,
                    remainingFree = 100,
                    hasEnoughBalance = true
                },
                message = "Kiểm tra chi phí thành công."
            });
        }

        /// <summary>
        /// Thiết lập phỏng vấn — AI phân loại JD
        /// POST /api/ai-interview/setup
        /// Tự động detect JSON body (text/url) hoặc FormData (file upload)
        /// </summary>
        [HttpPost("ai-interview/setup")]
        public async Task<IActionResult> SetupInterview()
        {
            try
            {
                string? jdText = null;
                var contentType = Request.ContentType ?? "";

                if (contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
                    // FormData — file upload
                    var file = Request.Form.Files.FirstOrDefault();
                    if (file != null && file.Length > 0)
                    {
                        using var reader = new StreamReader(file.OpenReadStream());
                        jdText = await reader.ReadToEndAsync();
                    }
                    else
                    {
                        jdText = Request.Form["jobDescriptionText"].FirstOrDefault();
                    }
                }
                else
                {
                    // JSON body
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    var json = JsonDocument.Parse(body).RootElement;

                    if (json.TryGetProperty("jobDescriptionText", out var jdProp))
                        jdText = jdProp.GetString();
                    else if (json.TryGetProperty("jobDescriptionUrl", out var urlProp))
                        return BadRequest(new { success = false, message = "Chức năng đọc JD từ URL đang được phát triển." });
                }

                if (string.IsNullOrWhiteSpace(jdText) || jdText.Length < 10)
                {
                    return BadRequest(new { success = false, message = "Nội dung JD quá ngắn hoặc trống." });
                }

                var result = await _interviewService.ClassifyJobDescriptionAsync(jdText);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Phân loại JD thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting up interview: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tạo phiên phỏng vấn mới
        /// POST /api/ai-interview/create-session
        /// </summary>
        [HttpPost("ai-interview/create-session")]
        public async Task<IActionResult> CreateSession([FromBody] CreateInterviewSessionRequest request)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var session = new InterviewSessionData
                {
                    AccountId = accountId.Value,
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
                    sessionId, accountId.Value);

                return Ok(new
                {
                    success = true,
                    data = new { sessionId, language = request.Language ?? "vi-VN" },
                    message = "Tạo phiên phỏng vấn thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating interview session: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lấy lời chào AI phỏng vấn viên
        /// GET /api/ai-interview/welcome-message/{sessionId}
        /// </summary>
        [HttpGet("ai-interview/welcome-message/{sessionId}")]
        public async Task<IActionResult> GetWelcomeMessage(int sessionId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _dataProvider.GetSessionByIdAsync(sessionId);
                if (session == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy phiên phỏng vấn {sessionId}" });

                var welcomeMessage = await _interviewService.GenerateWelcomeMessageAsync(
                    session.PositionName, session.CompanyName);

                string? audioBase64 = null;
                string? mimeType = null;
                try
                {
                    var speechResult = await _speechSynthesisService.SynthesizeToBase64Async(
                        welcomeMessage,
                        language: "vi-VN",
                        cancellationToken: cancellationToken);

                    audioBase64 = speechResult.AudioBase64;
                    mimeType = speechResult.MimeType;
                }
                catch (Exception ttsEx)
                {
                    _logger.LogWarning(ttsEx, "Lỗi khi gọi TTS cho lời chào");
                }

                return Ok(new
                {
                    success = true,
                    data = new { welcomeMessage, audioBase64, mimeType },
                    message = "Tạo lời chào thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating welcome message: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tạo câu hỏi phỏng vấn tiếp theo (adaptive)
        /// POST /api/ai-interview/generate-question
        /// </summary>
        [HttpPost("ai-interview/generate-question")]
        public async Task<IActionResult> GenerateQuestion([FromBody] GenerateQuestionRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var session = await _dataProvider.GetSessionByIdAsync(request.InterviewSessionId);
                if (session == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy phiên phỏng vấn {request.InterviewSessionId}" });

                if (session.AccountId != accountId.Value)
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên phỏng vấn này." });

                var result = await _interviewService.GenerateQuestionAsync(
                    request.InterviewSessionId, request.EstimatedAbility);

                if (result.IsTerminated)
                {
                    try
                    {
                        var speechTermResult = await _speechSynthesisService.SynthesizeToBase64Async(
                            result.TerminationMessage ?? "Buổi phỏng vấn kết thúc.",
                            language: "vi-VN",
                            cancellationToken: cancellationToken);
                        result.AudioBase64 = speechTermResult.AudioBase64;
                        result.MimeType = speechTermResult.MimeType;
                    } 
                    catch (Exception ttsEx)
                    {
                        _logger.LogWarning(ttsEx, "Lỗi TTS thông báo kết thúc phỏng vấn");
                    }

                    return Ok(new
                    {
                        success = true,
                        isTerminated = true,
                        terminationReason = result.TerminationReason,
                        terminationMessage = result.TerminationMessage,
                        data = result,
                        message = "Phỏng vấn đã kết thúc."
                    });
                }

                try
                {
                    var speechResult = await _speechSynthesisService.SynthesizeToBase64Async(
                        result.QuestionText,
                        language: "vi-VN",
                        cancellationToken: cancellationToken);

                    result.AudioBase64 = speechResult.AudioBase64;
                    result.MimeType = speechResult.MimeType;
                }
                catch (Exception ttsEx)
                {
                    _logger.LogWarning(ttsEx, "Lỗi TTS cho câu hỏi phỏng vấn");
                }

                return Ok(new { success = true, data = result, message = "Tạo câu hỏi thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating question: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lưu câu trả lời người dùng
        /// POST /api/ai-interview/submit-answer
        /// </summary>
        [HttpPost("ai-interview/submit-answer")]
        public async Task<IActionResult> SubmitAnswer([FromBody] SubmitAnswerRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var session = await _dataProvider.GetSessionByIdAsync(request.InterviewSessionId);
                if (session == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy phiên phỏng vấn {request.InterviewSessionId}" });

                if (session.AccountId != accountId.Value)
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });

                var response = await _dataProvider.GetResponseByIdAsync(request.InterviewResponseId);
                if (response == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy câu hỏi {request.InterviewResponseId}" });

                response.UserAnswer = request.UserAnswer;
                response.AnswerTimestamp = DateTimeOffset.UtcNow;
                await _dataProvider.UpdateResponseAsync(response);

                session.TotalQuestionsAnswered += 1;
                await _dataProvider.UpdateSessionAsync(session);

                // Tạo phản hồi AI cho câu trả lời (tương tác tự nhiên)
                string? aiReaction = null;
                string? aiReactionAudioBase64 = null;
                string? mimeType = null;

                try
                {
                    aiReaction = await _interviewService.GenerateReactionAsync(
                        request.InterviewSessionId,
                        response.QuestionContent,
                        request.UserAnswer);

                    if (!string.IsNullOrEmpty(aiReaction))
                    {
                        var speechResult = await _speechSynthesisService.SynthesizeToBase64Async(
                            aiReaction,
                            language: "vi-VN",
                            cancellationToken: cancellationToken);

                        aiReactionAudioBase64 = speechResult.AudioBase64;
                        mimeType = speechResult.MimeType;
                    }
                }
                catch (Exception reactionEx)
                {
                    _logger.LogWarning(reactionEx, "Lỗi khi gọi dịch vụ tạo câu phản hồi hoặc TTS, bỏ qua.");
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        message = "Câu trả lời đã được ghi nhận.",
                        aiReaction = aiReaction,
                        aiReactionAudioBase64 = aiReactionAudioBase64,
                        mimeType = mimeType
                    },
                    message = "Gửi câu trả lời thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting answer: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Kết thúc phỏng vấn — tạo feedback chạy nền
        /// POST /api/ai-interview/end-interview/{sessionId}
        /// </summary>
        [HttpPost("ai-interview/end-interview/{sessionId}")]
        public async Task<IActionResult> EndInterview(int sessionId)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var session = await _dataProvider.GetSessionByIdAsync(sessionId);
                if (session == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy phiên phỏng vấn {sessionId}" });

                if (session.AccountId != accountId.Value)
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });

                if (session.Status == "Completed")
                    return Ok(new { success = true, data = new { sessionId, message = "Phỏng vấn đã hoàn thành." }, message = "Phỏng vấn đã hoàn thành." });

                session.EndTime = DateTimeOffset.UtcNow;
                await _dataProvider.UpdateSessionAsync(session);

                // Chạy nền tạo feedback
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var svc = scope.ServiceProvider.GetRequiredService<IInterviewService>();
                        var dp = scope.ServiceProvider.GetRequiredService<IInterviewSessionDataProvider>();
                        var log = scope.ServiceProvider.GetRequiredService<ILogger<AIInterviewController>>();

                        log.LogInformation("Background feedback started for session {SessionId}", sessionId);
                        var overallFeedback = await svc.GenerateFeedbackForSessionAsync(sessionId);

                        var s = await dp.GetSessionByIdAsync(sessionId);
                        if (s != null)
                        {
                            s.Status = "Completed";
                            s.OverallFeedback = overallFeedback;
                            await dp.UpdateSessionAsync(s);
                        }
                        log.LogInformation("Background feedback completed for session {SessionId}", sessionId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background feedback error for session {SessionId}", sessionId);
                    }
                });

                return Ok(new
                {
                    success = true,
                    data = new { sessionId, status = "Processing", message = "Phỏng vấn đã kết thúc. Kết quả đang được tạo." },
                    message = "Kết thúc phỏng vấn thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending interview: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Xem kết quả phỏng vấn chi tiết
        /// GET /api/ai-interview/result/{sessionId}
        /// </summary>
        [HttpGet("ai-interview/result/{sessionId}")]
        public async Task<IActionResult> GetInterviewResult(int sessionId)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var session = await _dataProvider.GetSessionByIdAsync(sessionId);
                if (session == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy phiên phỏng vấn {sessionId}" });

                if (session.AccountId != accountId.Value)
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });

                var allResponses = await _dataProvider.GetResponsesBySessionIdAsync(sessionId);
                var answered = allResponses.Where(r => !string.IsNullOrEmpty(r.UserAnswer)).OrderBy(r => r.TurnNumber).ToList();
                var withFeedback = answered.Where(r => !string.IsNullOrEmpty(r.StructuredFeedbackJson))
                    .Select((r, i) => new
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

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        session = new
                        {
                            id = session.Id, positionName = session.PositionName, skillName = session.SkillName,
                            levelName = session.LevelName, companyName = session.CompanyName,
                            startTime = session.StartTime, endTime = session.EndTime,
                            status = session.Status, totalQuestions = answered.Count,
                            totalQuestionsAnswered = withFeedback.Count,
                            overallFeedback = session.OverallFeedback, estimatedAbility = session.EstimatedAbility
                        },
                        responses = withFeedback
                    },
                    message = "Lấy kết quả phỏng vấn thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview result: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Khôi phục trạng thái phiên phỏng vấn khi reload trang
        /// GET /api/ai-interview/resume-session/{sessionId}
        /// Trả về session info + tất cả responses (kể cả chưa feedback) để frontend rebuild chat
        /// </summary>
        [HttpGet("ai-interview/resume-session/{sessionId}")]
        public async Task<IActionResult> ResumeSession(int sessionId)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var session = await _dataProvider.GetSessionByIdAsync(sessionId);
                if (session == null)
                    return NotFound(new { success = false, message = $"Không tìm thấy phiên phỏng vấn {sessionId}" });

                if (session.AccountId != accountId.Value)
                    return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });

                var allResponses = await _dataProvider.GetResponsesBySessionIdAsync(sessionId);
                var orderedResponses = allResponses.OrderBy(r => r.TurnNumber).ToList();

                // Xác định câu hỏi cuối cùng chưa được trả lời (nếu có)
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

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        session = new
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
                        responses = responseList,
                        answeredCount,
                        currentResponseId = lastUnanswered?.Id,
                        hasUnansweredQuestion = lastUnanswered != null,
                    },
                    message = "Khôi phục phiên phỏng vấn thành công."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resuming session: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        /// <summary>
        /// Danh sách lịch sử phỏng vấn

        /// GET /api/ai-interview/history
        /// </summary>
        [HttpGet("ai-interview/history")]
        public async Task<IActionResult> GetInterviewHistory()
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var sessions = await _dataProvider.GetSessionsByAccountIdAsync(accountId.Value);
                var history = sessions.Select(s => new
                {
                    id = s.Id, positionName = s.PositionName, skillName = s.SkillName,
                    levelName = s.LevelName, companyName = s.CompanyName,
                    startTime = s.StartTime, endTime = s.EndTime,
                    totalQuestionsAnswered = s.TotalQuestionsAnswered,
                    estimatedAbility = s.EstimatedAbility, status = s.Status,
                    interviewType = s.QuestionId != null ? "Single_Question" : (s.UserCvId != null ? "CV_JD" : "Text"),
                    questionContent = (string?)null, difficulty = (string?)null, isFromSystem = (bool?)null
                }).ToList();

                return Ok(new { success = true, data = history, message = "Lấy lịch sử phỏng vấn thành công." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting interview history: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        private int? GetAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("accountId")?.Value;
            return claim != null && int.TryParse(claim, out int id) ? id : null;
        }
    }
}
