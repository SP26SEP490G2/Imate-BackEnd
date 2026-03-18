using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Imate.AI.Module.Controllers
{
    /// <summary>
    /// Controller cho Practice Test AI
    /// UC-30: Practice Test
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class PracticeTestController : ControllerBase
    {
        private readonly IPracticeTestService _practiceTestService;
        private readonly ILogger<PracticeTestController> _logger;

        public PracticeTestController(IPracticeTestService practiceTestService, ILogger<PracticeTestController> logger)
        {
            _practiceTestService = practiceTestService;
            _logger = logger;
        }

        /// <summary>
        /// Sinh bài test luyện tập bằng AI
        /// POST /api/ai/practice-test/generate
        /// </summary>
        [HttpPost("ai/practice-test/generate")]
        public async Task<IActionResult> GenerateTest([FromBody] GeneratePracticeTestRequest request)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var result = await _practiceTestService.GenerateTestAsync(accountId.Value, request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Tạo bài test thành công."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating practice test: {Message}", ex.Message);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi tạo bài test. Vui lòng thử lại sau."
                });
            }
        }

        private int? GetAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("accountId")?.Value;

            if (claim != null && int.TryParse(claim, out int accountId))
                return accountId;

            return null;
        }
    }
}
