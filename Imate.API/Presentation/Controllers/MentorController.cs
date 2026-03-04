using Imate.API.Business.Interfaces;
using Imate.API.Common.Router;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
                return Ok(new
                {
                    data = await _mentorService.GetListPreviewMentors()
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    data = (object)null
                });
            }
        }
    }
}
