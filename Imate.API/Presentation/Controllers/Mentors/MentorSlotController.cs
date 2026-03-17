using Imate.API.Business.Interfaces.Mentors;
using Microsoft.AspNetCore.Mvc;

namespace Imate.API.Presentation.Controllers.Mentors
{
    [ApiController]
    [Route("api/mentor-recurring-slot")]
    public class MentorSlotController : ControllerBase
    {
        private readonly IMentorSlotService _mentorSlotService;

        public MentorSlotController(IMentorSlotService mentorSlotService)
        {
            _mentorSlotService = mentorSlotService;
        }

        [HttpGet("mentor/{mentorId}")]
        public async Task<IActionResult> GetMentorRecurringSlots(int mentorId)
        {
            var result = await _mentorSlotService.GetMentorRecurringSlotsAsync(mentorId);
            return Ok(result);
        }
    }
}
