using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.Common.Router;
using Imate.API.Presentation.RequestModels.Recruiters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Recruiters
{
    [ApiController]
    [Route("api")]
    [Authorize]
    public class RecruiterController : ControllerBase
    {
        private readonly IRecruiterService _recruiterService;

        public RecruiterController(IRecruiterService recruiterService)
        {
            _recruiterService = recruiterService;
        }

        [HttpPost(APIConfig.Recruiter.SubmitRecruiterProfile)]
        public async Task<IActionResult> SubmitRecruiterProfile([FromBody] SubmitRecruiterProfileRequest request)
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("accountId")?.Value;

                if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                await _recruiterService.SubmitRecruiterProfileAsync(accountId, request);

                return Ok(new { message = "Nộp hồ sơ Recruiter thành công. Vui lòng chờ hệ thống duyệt." });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    data = (object?)null,
                    message = ex.Message
                });
            }
        }
    }
}
