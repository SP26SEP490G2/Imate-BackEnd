using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Security.Claims;
using System.Text.Json;


namespace Imate.AI.Module.API.Controllers
{
    /// <summary>
    /// Controller phỏng vấn AI : Practice Mock Interview (Tầng 1 - Controllers)
    /// Thin controller: chỉ xử lý HTTP concerns, delegate business logic cho Orchestrator
    /// Route: /api/ai-interview/*
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class AIInterviewController : ControllerBase
    {
        private readonly IInterviewOrchestrator _orchestrator;
        private readonly ILogger<AIInterviewController> _logger;

        public AIInterviewController(
            IInterviewOrchestrator orchestrator,
            ILogger<AIInterviewController> logger)
        {
            _orchestrator = orchestrator;
            _logger = logger;
        }

        /// <summary>
        /// Kiểm tra chi phí phỏng vấn (lượt free / subscription)
        /// GET /api/ai-interview/check-interview-cost
        /// </summary>
        [HttpGet("ai-interview/check-interview-cost")]
        public async Task<IActionResult> CheckInterviewCost()
        {
            var accountId = GetAccountId();
            if (accountId == null)
                return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

            var status = await _orchestrator.CheckInterviewCostAsync(accountId.Value);

            return Ok(new
            {
                success = true,
                data = new
                {
                    requiresPayment = !status.CanStart && status.IsFree,
                    isFree = status.IsFree,
                    usedMock = status.UsedCount,
                    limit = status.LimitCount,
                    remaining = status.RemainingCount,
                    canStart = status.CanStart,
                    message = status.Message
                },
                message = "Kiểm tra chi phí thành công."
            });
        }

        /// <summary>
        /// Thiết lập phỏng vấn — AI phân loại JD + validate CV
        /// POST /api/ai-interview/setup
        /// </summary>
        [HttpPost("ai-interview/setup")]
        public async Task<IActionResult> SetupInterview()
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                string? jdText = null;
                int? cvId = null;
                var contentType = Request.ContentType ?? "";

                if (contentType.Contains("multipart/form-data", StringComparison.OrdinalIgnoreCase))
                {
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

                    // Parse cvId from form
                    var cvIdStr = Request.Form["cvId"].FirstOrDefault();
                    if (!string.IsNullOrEmpty(cvIdStr) && int.TryParse(cvIdStr, out var parsedCvId))
                        cvId = parsedCvId;
                }
                else
                {
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    var json = JsonDocument.Parse(body).RootElement;

                    if (json.TryGetProperty("jobDescriptionText", out var jdProp))
                        jdText = jdProp.GetString();
                    else if (json.TryGetProperty("jobDescriptionUrl", out var urlProp))
                        return BadRequest(new { success = false, message = "Chức năng đọc JD từ URL đang được phát triển." });

                    // Parse cvId from JSON
                    if (json.TryGetProperty("cvId", out var cvIdProp) && cvIdProp.ValueKind == JsonValueKind.Number)
                        cvId = cvIdProp.GetInt32();
                }

                if (string.IsNullOrWhiteSpace(jdText) || jdText.Length < 10)
                {
                    return BadRequest(new { success = false, message = "Nội dung JD quá ngắn hoặc trống." });
                }

                var result = await _orchestrator.SetupInterviewAsync(accountId.Value, jdText, cvId);

                return Ok(new { success = true, data = result, message = "Phân loại JD thành công." });
            }
            catch (InvalidOperationException ex)
            {
                // Validation error (CV không IT, level gap quá lớn)
                _logger.LogWarning("[SETUP] Validation failed: {Message}", ex.Message);
                return BadRequest(new { success = false, message = ex.Message });
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

                var sessionId = await _orchestrator.CreateSessionAsync(accountId.Value, request);

                return Ok(new
                {
                    success = true,
                    data = new { sessionId, language = request.Language ?? "vi-VN" },
                    message = "Tạo phiên phỏng vấn thành công."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để thao tác." });

                var result = await _orchestrator.GetWelcomeMessageAsync(accountId.Value, sessionId, cancellationToken);

                return Ok(new
                {
                    success = true,
                    data = new { welcomeMessage = result.WelcomeMessage, audioBase64 = result.AudioBase64, mimeType = result.MimeType },
                    message = "Tạo lời chào thành công."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(403, new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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

                var result = await _orchestrator.GenerateQuestionAsync(
                    accountId.Value, request.InterviewSessionId, request.EstimatedAbility, cancellationToken);

                if (result.IsTerminated)
                {
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

                return Ok(new { success = true, data = result, message = "Tạo câu hỏi thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên phỏng vấn này." });
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

                var result = await _orchestrator.SubmitAnswerAsync(accountId.Value, request, cancellationToken);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        message = "Câu trả lời đã được ghi nhận.",
                        aiReaction = result.AiReaction,
                        aiReactionAudioBase64 = result.AiReactionAudioBase64,
                        mimeType = result.MimeType
                    },
                    message = "Gửi câu trả lời thành công."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });
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

                await _orchestrator.EndInterviewAsync(accountId.Value, sessionId);

                return Ok(new
                {
                    success = true,
                    data = new { sessionId, status = "Processing", message = "Phỏng vấn đã kết thúc. Kết quả đang được tạo." },
                    message = "Kết thúc phỏng vấn thành công."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });
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

                var result = await _orchestrator.GetInterviewResultAsync(accountId.Value, sessionId);

                return Ok(new { success = true, data = result, message = "Lấy kết quả phỏng vấn thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });
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
        /// </summary>
        [HttpGet("ai-interview/resume-session/{sessionId}")]
        public async Task<IActionResult> ResumeSession(int sessionId)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var result = await _orchestrator.ResumeSessionAsync(accountId.Value, sessionId);

                return Ok(new { success = true, data = result, message = "Khôi phục phiên phỏng vấn thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(403, new { success = false, message = "Bạn không có quyền truy cập phiên này." });
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

                var history = await _orchestrator.GetInterviewHistoryAsync(accountId.Value);

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