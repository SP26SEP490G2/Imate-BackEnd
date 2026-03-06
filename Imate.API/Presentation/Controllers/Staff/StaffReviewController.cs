using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Interfaces.Staff;
using System.Security.Claims;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.Controllers.Staff
{
    [Route("api/staff-review")]
    [ApiController]
    [Authorize(Roles = "Staff")] // Chỉ Staff mới được phép truy cập
    public class StaffReviewController : ControllerBase
    {
        private readonly IStaffReviewService _staffReviewService;

        public StaffReviewController(IStaffReviewService staffReviewService)
        {
            _staffReviewService = staffReviewService;
        }

        [HttpGet("mentors/pending")]
        public async Task<IActionResult> GetPendingMentors()
        {
            var result = await _staffReviewService.GetPendingMentorApplicationsAsync();
            return Ok(result);
        }

        [HttpGet("recruiters/pending")]
        public async Task<IActionResult> GetPendingRecruiters()
        {
            var result = await _staffReviewService.GetPendingRecruiterApplicationsAsync();
            return Ok(result);
        }

        [HttpPost("mentors/{id}/review")]
        public async Task<IActionResult> ReviewMentor(int id, [FromBody] ReviewRequest request)
        {
            var staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _staffReviewService.ReviewMentorApplicationAsync(id, request.IsApproved, request.Note, staffId);
            return Ok(new { Message = $"{(request.IsApproved ? "Duyệt" : "Từ chối")} hồ sơ Mentor thành công." });
        }

        [HttpPost("recruiters/{id}/review")]
        public async Task<IActionResult> ReviewRecruiter(int id, [FromBody] ReviewRequest request)
        {
            var staffId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await _staffReviewService.ReviewRecruiterApplicationAsync(id, request.IsApproved, request.Note, staffId);
            return Ok(new { Message = $"{(request.IsApproved ? "Duyệt" : "Từ chối")} hồ sơ Recruiter thành công." });
        }

    }

    public class ReviewRequest
    {
        public bool IsApproved { get; set; }
        public string? Note { get; set; }
    }
}
