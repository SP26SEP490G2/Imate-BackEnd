using Imate.API.Business.Interfaces;
using Imate.API.Presentation.RequestModels.PracticeTest;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers
{
    /// <summary>
    /// Controller cho Practice Test History
    /// UC-31: View Test Result History
    /// UC-32: View Test Result History Detail
    /// </summary>
    [ApiController]
    [Route("api/practice-test")]
    [Authorize]
    public class PracticeTestHistoryController : ControllerBase
    {
        private readonly IPracticeTestHistoryService _historyService;

        public PracticeTestHistoryController(IPracticeTestHistoryService historyService)
        {
            _historyService = historyService;
        }

        /// <summary>
        /// Nộp bài test và lưu kết quả
        /// POST /api/practice-test/submit
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitTest([FromBody] SubmitPracticeTestRequest request)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var sessionId = await _historyService.SubmitTestAsync(accountId.Value, request);

                return Ok(new
                {
                    success = true,
                    data = new { sessionId },
                    message = "Lưu kết quả bài test thành công."
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lưu kết quả. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// UC-31: Lấy danh sách lịch sử bài test
        /// GET /api/practice-test/history
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory()
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var history = await _historyService.GetHistoryAsync(accountId.Value);

                return Ok(new
                {
                    success = true,
                    data = history,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Không thể tải thông tin. Vui lòng thử lại." });
            }
        }

        /// <summary>
        /// UC-32: Lấy chi tiết kết quả bài test
        /// GET /api/practice-test/history/{id}
        /// </summary>
        [HttpGet("history/{id}")]
        public async Task<IActionResult> GetDetail(int id)
        {
            try
            {
                var accountId = GetAccountId();
                if (accountId == null)
                    return Unauthorized(new { success = false, message = "Không thể xác định thông tin người dùng." });

                var detail = await _historyService.GetDetailAsync(accountId.Value, id);

                return Ok(new
                {
                    success = true,
                    data = detail,
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Không thể tải thông tin. Vui lòng thử lại." });
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
