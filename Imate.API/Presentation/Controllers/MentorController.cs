using Imate.API.Business.Interfaces;
using Imate.API.Common.Router;
using Microsoft.AspNetCore.Mvc;

namespace Imate.API.Presentation.Controllers
{
    [ApiController]
    [Route("api")]
    public class MentorController : Controller
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
                var data = await _mentorService.GetListPreviewMentors();
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
