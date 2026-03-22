using System.Security.Claims;
using Imate.API.Business.Interfaces.Mentors;
using Imate.API.Business.Interfaces.Recruiters;
using Imate.API.Presentation.RequestModels.JobApplications;
using Imate.API.Presentation.RequestModels.Recruiters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Imate.API.Presentation.Controllers.Candidates
{
	[Route("api")]
	[ApiController]
	[Authorize]
	public class JobApplyController : ControllerBase
	{
		private readonly IRecruiterService _recruiterService;

		public JobApplyController(IRecruiterService recruiterService)
		{
			_recruiterService = recruiterService;
		}

		[AllowAnonymous]
		[HttpPost("apply-job")]
		public async Task<IActionResult> ApplyJob([FromBody] CreateJobApplicationRequest request)
		{
			try
			{
				var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? User.FindFirst("sub")?.Value
					?? User.FindFirst("accountId")?.Value;

				if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
					return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

				var result = await _recruiterService.CreateJobApplication(accountId, request);

				return Ok(new {message = "Apply Đơn Đăng tuyển thành công" });
			}
			catch (Exception ex)
			{
				return BadRequest(new
				{
					message = ex.Message
				});
			}
		}

		[HttpGet("get-applied-jobs")]
		public async Task<IActionResult> GetAppliedJob([FromQuery] AppliedApplicationCandidateFilterRequest request)
		{
			try
			{
				var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
					?? User.FindFirst("sub")?.Value
					?? User.FindFirst("accountId")?.Value;

				if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
					return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

				var result = await _recruiterService.GetCandidateAppliedJob(accountId, request);
				return Ok(new { data = result, message = "Lấy danh sách đơn đăng tuyển thành công." });

			}
			catch (Exception ex) {
				return BadRequest(new
				{
					message = ex.Message
				});
			}
		}
	}
}