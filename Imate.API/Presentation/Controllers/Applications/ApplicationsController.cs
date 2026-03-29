using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Applications;
using Imate.API.Presentation.RequestModels.Applications;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Applications
{
    [Route("api")]
    [ApiController]
    public class ApplicationsController : ControllerBase
    {
        private readonly IApplicationService _applicationService;
        public ApplicationsController(IApplicationService applicationService)
        {
            _applicationService = applicationService;
        }
        [HttpGet("applications/{id}")]
        public async Task<IActionResult> GetAllApplications(int id, [FromQuery] ApplicationParams applicationParams)
        {
            var pagedResult = await _applicationService.GetApplicationsByIdAsync(id, applicationParams);
            Response.Headers.Add("X-Pagination",
                System.Text.Json.JsonSerializer.Serialize(
            new
            {
                pagedResult.TotalCount,
                pagedResult.PageSize,
                pagedResult.PageNumber,
                pagedResult.TotalPages
            }));

            return Ok(pagedResult);
        }
        [HttpPost("application/technical-application/{candidateId}")]
        public async Task<IActionResult> CreateApplication(int candidateId, [FromForm] CreateTechnicalApplicationRequest request)
        {
            try
            {
                var applicationDetail = await _applicationService.CreateTechnicalApplicationAsync(request, candidateId);
                return StatusCode(StatusCodes.Status201Created, applicationDetail);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Không thể tạo đơn kỹ thuật.", Error = ex.Message });
            }
        }
        [HttpPost("application/report-application/{candidateId}")]
        public async Task<IActionResult> CreateReportApplication(int candidateId, [FromForm] CreateReportApplicationRequest request)
        {
            try
            {
                var applicationDetail = await _applicationService.CreateReportApplicationAsync(request, candidateId);
                return StatusCode(StatusCodes.Status201Created, applicationDetail);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { Message = "Không thể tạo đơn báo cáo.", Error = ex.Message });
            }
        }
        [HttpPost("application/report-comment/{userId}")]
        public async Task<IActionResult> CreateReportCommentApplication(int userId, [FromForm] CreateReportCommentRequest request)
        {
            try
            {
                var applicationDetail = await _applicationService.CreateReportCommentApplicationAsync(request, userId);
                return StatusCode(StatusCodes.Status201Created, applicationDetail);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể tạo đơn report comment.", error = ex.Message });
            }
        }
        [HttpGet("applications")]
        public async Task<IActionResult> GetAllApplications([FromQuery] Application2Params applicationParams)
        {
            var pagedResult = await _applicationService.GetAllApplicationsAsync(applicationParams);
            Response.Headers.Add("X-Pagination",
               System.Text.Json.JsonSerializer.Serialize(
            new
            {
                pagedResult.TotalCount,
                pagedResult.PageSize,
                pagedResult.PageNumber,
                pagedResult.TotalPages
            }));

            return Ok(pagedResult);

        }

        [HttpGet("applications/pending-summary")]
        public async Task<IActionResult> GetPendingSummary()
        {
            var summary = await _applicationService.GetPendingSummaryAsync();
            return Ok(new
            {
                success = true,
                data = summary,
                message = "Lấy thống kê đơn cần duyệt thành công"
            });
        }
        [HttpGet("application/{applicationId}")]
        public async Task<IActionResult> GetApplicationDetails(int applicationId)
        {
            var applicationDetails = await _applicationService.GetApplicationDetails(applicationId);
            return Ok(applicationDetails);
        }
        [HttpGet("application/{applicationId}/report-rating-details")]
        public async Task<IActionResult> GetReportRatingDetails(int applicationId)
        {
            var details = await _applicationService.GetReportRatingDetails(applicationId);
            return Ok(details);
        }
        [HttpGet("application/{applicationId}/report-mentor-details")]
        public async Task<IActionResult> GetReportMentorDetails(int applicationId)
        {

            var details = await _applicationService.GetReportMentorDetails(applicationId);
            return Ok(details);

        }
        [HttpGet("application/{applicationId}/technical-details")]
        public async Task<IActionResult> GetTechnicalDetails(int applicationId)
        {

            var details = await _applicationService.GetTechnicalDetails(applicationId);
            return Ok(details);

        }

        [HttpGet("application/{applicationId}/report-comment-details")]
        public async Task<IActionResult> GetReportCommentDetails(int applicationId)
        {
            try
            {
                var details = await _applicationService.GetReportCommentDetails(applicationId);
                return Ok(details);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể lấy chi tiết đơn.", error = ex.Message });
            }
        }

        [HttpPut("application/{applicationId}/approve")]
        public async Task<IActionResult> ApproveApplication(int applicationId, [FromBody] ApplicationResponseRequest? request = null)
        {
            var reviewerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(reviewerIdClaim) || !int.TryParse(reviewerIdClaim, out int reviewerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ." });
            }

            try
            {
                await _applicationService.ApproveApplicationAsync(applicationId, reviewerId, request?.ResponseNote);
                return Ok(new { message = "Đơn đã được duyệt thành công." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể duyệt đơn.", error = ex.Message });
            }
        }

        [HttpPut("application/{applicationId}/reject")]
        public async Task<IActionResult> RejectApplication(int applicationId, [FromBody] ApplicationResponseRequest? request = null)
        {
            var reviewerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(reviewerIdClaim) || !int.TryParse(reviewerIdClaim, out int reviewerId))
            {
                return Unauthorized(new { message = "Token không hợp lệ." });
            }

            try
            {
                await _applicationService.RejectApplicationAsync(applicationId, reviewerId, request?.ResponseNote);
                return Ok(new { message = "Đơn đã bị từ chối." });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Không thể từ chối đơn.", error = ex.Message });
            }
        }
    }
}
