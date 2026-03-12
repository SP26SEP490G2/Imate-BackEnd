using Azure.Core;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.Common.Router;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
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
        private readonly IAuditLogService _auditLogService;

        public RecruiterController(IRecruiterService recruiterService, IAuditLogService auditLogService)
        {
            _recruiterService = recruiterService;
            _auditLogService = auditLogService;
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
        [HttpGet("recruiter-job-applications")]
        public async Task<IActionResult> getJobList([FromQuery] RecruiterJobSearchFilterRequest? request)
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("accountId")?.Value;

                if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                var result = await _recruiterService.GetListJobRecruiterAsync(accountId, request);

                return Ok(new { data=result, message = "Lấy danh sách đơn đăng tuyển thành công." });
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

        [HttpPost("create-job-posts")]
        public async Task<IActionResult> createJobPost([FromBody] CreateUpdateJobRequest request)
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("accountId")?.Value;

                if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                 var newjob = await _recruiterService.CreateJobPostAsync(accountId, request);
                return Ok(new {message ="Tạo Đơn Đăng Tuyển thành công"});
            } catch(Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("update-job-applications")]
        public async Task<IActionResult> UpdateJobPost([FromBody] CreateUpdateJobRequest request)
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("accountId")?.Value;

                if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                var newjob = await _recruiterService.UpdateJobPostAsync(accountId, request);
                return Ok(new { message = "Update Đơn Đăng Tuyển thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpPut("close-job-applications")]
        public async Task<IActionResult> CloseJobPost([FromBody] int jobId)
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? User.FindFirst("sub")?.Value
                    ?? User.FindFirst("accountId")?.Value;

                if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                var newjob = await _recruiterService.CloseJobPostAsync(accountId, jobId);
                return Ok(new { message = "Update Đơn Đăng Tuyển thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
