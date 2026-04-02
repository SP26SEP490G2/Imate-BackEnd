using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces.Staff;
using Imate.API.Business.Helper;
using Imate.API.Presentation.ResponseModels.Staff;
using System.Security.Claims;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.Controllers.Staff
{
    [Route("api/staff-review")]
    [ApiController]
    [AllowAnonymous]
    public class StaffReviewController : ControllerBase
    {
        private readonly IStaffReviewService _staffReviewService;

        public StaffReviewController(IStaffReviewService staffReviewService)
        {
            _staffReviewService = staffReviewService;
        }

        /// <summary>Danh sách đơn Mentor chờ duyệt (phân trang).</summary>
        [AllowAnonymous]
        [HttpGet("mentors/pending")]
        [ProducesResponseType(typeof(PagedList<StaffMentorApplicationResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingMentors(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 6,
            [FromQuery] string? searchTerm = null)
        {
            if (pageSize <= 0 || pageSize > 100) pageSize = 6;
            if (pageNumber <= 0) pageNumber = 1;
            try
            {
                var result = await _staffReviewService.GetPendingMentorApplicationsPagedAsync(pageNumber, pageSize, searchTerm);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        [HttpGet("mentors/{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(StaffMentorApplicationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMentorById(int id)
        {
            var mentor = await _staffReviewService.GetMentorApplicationByIdAsync(id);
            if (mentor == null)
                return NotFound();
            return Ok(mentor);
        }

        [HttpGet("recruiters/pending")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPendingRecruiters()
        {
            var result = await _staffReviewService.GetPendingRecruiterApplicationsAsync();
            return Ok(result);
        }

        [HttpPost("mentors/{id}/review")]
        [AllowAnonymous]
        public async Task<IActionResult> ReviewMentor(int id, [FromBody] ReviewRequest request)
        {
            var staffId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var sid) ? sid : 0;
            await _staffReviewService.ReviewMentorApplicationAsync(id, request.IsApproved, request.Note, staffId);
            return Ok(new { Message = $"{(request.IsApproved ? "Duyệt" : "Từ chối")} hồ sơ Mentor thành công." });
        }

        [HttpPost("recruiters/{id}/review")]
        [AllowAnonymous]
        public async Task<IActionResult> ReviewRecruiter(int id, [FromBody] ReviewRequest request)
        {
            var staffId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var sid) ? sid : 0;
            await _staffReviewService.ReviewRecruiterApplicationAsync(id, request.IsApproved, request.Note, staffId, request.CreateCompany ?? false);
            return Ok(new { Message = $"{(request.IsApproved ? "Duyệt" : "Từ chối")} hồ sơ Recruiter thành công." });
        }

    }

    public class ReviewRequest
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
        public bool? CreateCompany { get; set; }
    }
}
