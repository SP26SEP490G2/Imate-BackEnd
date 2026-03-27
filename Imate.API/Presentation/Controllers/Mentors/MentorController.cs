using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Mentors;
using Imate.API.Common.Router;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Imate.API.Presentation.RequestModels.Mentors;

namespace Imate.API.Presentation.Controllers.Mentors
{
    [ApiController]
    [Route("api")]
    public class MentorController : ControllerBase
    {
        private readonly IMentorService _mentorService;

        public MentorController(IMentorService mentorService)
        {
            _mentorService = mentorService;
        }

        [HttpGet(APIConfig.Mentor.GetListPreviewMentors)]
        public async Task<IActionResult> GetListPreviewMentors([FromQuery] CommonParams mentorParams)
        {
            try
            {
                var pagedResult = await _mentorService.GetListPreviewMentorsAsync(mentorParams);

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
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    data = (object?)null,
                    message = ex.Message
                });
            }
        }

        [HttpGet(APIConfig.Mentor.GetMyCandidateRatings)]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> GetMyCandidateRatings()
        {
            try
            {
                var mentorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _mentorService.GetCandidateRatingsAsync(mentorId);
                return Ok(result);
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

        [HttpPut(APIConfig.Mentor.UpdatePrice)]
        [Authorize(Roles = "Mentor")]
        public async Task<IActionResult> UpdatePrice([FromBody] UpdateMentorPriceRequest request)
        {
            try
            {
                var mentorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _mentorService.UpdateMentorPriceAsync(mentorId, request.PricePerSession);
                return Ok(new { message = "Cập nhật giá thành công." });
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
