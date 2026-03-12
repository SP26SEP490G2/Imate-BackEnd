using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Imate.AI.Module.Controllers
{
    /// <summary>
    /// Controller cho các chức năng AI liên quan đến CV
    /// </summary>
    [ApiController]
    [Route("api")]
    [Authorize]
    public class CvAnalysisController : ControllerBase
    {
        private readonly ICvAnalysisService _cvAnalysisService;
        private readonly ILogger<CvAnalysisController> _logger;

        public CvAnalysisController(ICvAnalysisService cvAnalysisService, ILogger<CvAnalysisController> logger)
        {
            _cvAnalysisService = cvAnalysisService;
            _logger = logger;
        }

        /// <summary>
        /// Phân tích CV bằng Gemini AI
        /// UC-29: Analyse CV
        /// </summary>
        [HttpPost("ai/analyse-cv")]
        public async Task<IActionResult> AnalyseCv([FromBody] AnalyseCvRequest request)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var result = await _cvAnalysisService.AnalyseCvAsync(accountId.Value, request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = "Phân tích CV thành công."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analysing CV: {Message}", ex.Message);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi phân tích CV. Vui lòng thử lại sau." });
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
