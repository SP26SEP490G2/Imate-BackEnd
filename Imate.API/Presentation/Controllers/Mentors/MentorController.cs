using Imate.API.Business.Interfaces.Mentors;
using Imate.API.Common.Router;
using Microsoft.AspNetCore.Mvc;

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
        public async Task<IActionResult> GetListPreviewMentors()
        {
            try
            {
                var mentors = await _mentorService.GetListPreviewMentorsAsync();
                return Ok(new
                {
                    data = mentors
                });
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
