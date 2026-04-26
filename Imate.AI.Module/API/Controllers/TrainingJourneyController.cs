using Imate.AI.Module.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.AI.Module.API.Controllers
{
    [ApiController]
    [Route("api/training-journey")]
    [Authorize]
    public class TrainingJourneyController : ControllerBase
    {
        private readonly ITrainingJourneyOrchestrator _orchestrator;

        public TrainingJourneyController(ITrainingJourneyOrchestrator orchestrator)
            => _orchestrator = orchestrator;
        [HttpPost]
        public async Task<IActionResult> CreateJourney([FromBody] CreateJourneyRequest request)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            var result = await _orchestrator.CreateJourneyAsync(accountId.Value, request.CvId, request.CvContent, request.JobDescriptionText, request.Name);

            return Ok(new { success = true, data = result });
        }

        [HttpPatch("{journeyId:int}/rename")]
        public async Task<IActionResult> RenameJourney(int journeyId, [FromBody] RenameJourneyRequest request)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.NewName))
                return BadRequest("Tên mới không được để trống");

            await _orchestrator.RenameJourneyAsync(accountId.Value, journeyId, request.NewName);
            return Ok(new { success = true, message = "Đổi tên lộ trình thành công" });
        }



        [HttpGet]
        public async Task<IActionResult> GetJourneyList([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var accountIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(accountIdStr) || !int.TryParse(accountIdStr, out int accountId))
            {
                return Unauthorized(new { Message = "User not found or invalid token." });
            }

            var result = await _orchestrator.GetJourneyListAsync(accountId, page, pageSize);
            return Ok(new { Message = "Lấy danh sách lộ trình thành công", Data = result });
        }

        [HttpGet("{journeyId:int}/progress")]
        public async Task<IActionResult> GetProgress(int journeyId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            var result = await _orchestrator.GetProgressAsync(accountId.Value, journeyId);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("{journeyId:int}/start-session")]
        public async Task<IActionResult> StartSession(int journeyId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            var result = await _orchestrator.StartSessionAsync(accountId.Value, journeyId);

            if (result.AllResolved)
                return Ok(new { success = true, allResolved = true, message = "Bạn đã thành thạo tất cả kỹ năng! 🎉" });

            return Ok(new { success = true, data = result });
        }

        [HttpPost("end-session/{sessionId:int}")]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            var accountId = GetAccountId();
            if (accountId == null) return Unauthorized();

            var result = await _orchestrator.EndSessionAsync(accountId.Value, sessionId);
            return Ok(new { success = true, data = result });
        }

        private int? GetAccountId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? User.FindFirst("accountId")?.Value;
            return claim != null && int.TryParse(claim, out int id) ? id : null;
        }
    }

    public class CreateJourneyRequest
    {
        public int CvId { get; set; }
        public string CvContent { get; set; } = string.Empty;
        public string JobDescriptionText { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    public class RenameJourneyRequest
    {
        public string NewName { get; set; } = string.Empty;
    }


}