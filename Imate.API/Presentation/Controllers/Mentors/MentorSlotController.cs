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

        [HttpGet("slots")]
        public async Task<IActionResult> GetAllSlots()
        {
            var result = await _mentorSlotService.GetAllSlotsAsync();
            return Ok(new
            {
                success = true,
                message = "Lấy danh sách tất cả slots thành công",
                data = result
            });
        }

        [HttpGet("my-slots")]
        public async Task<IActionResult> GetMyRecurringSlots()
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { success = false, message = "Không tìm thấy thông tin người dùng" });
            }

            var result = await _mentorSlotService.GetMentorRecurringSlotsAsync(accountId);
            return Ok(new
            {
                success = true,
                message = "Lấy danh sách slots thành công",
                data = result
            });
        }

        [HttpGet("mentor/{mentorId}")]
        public async Task<IActionResult> GetMentorRecurringSlots(int mentorId)
        {
            var result = await _mentorSlotService.GetMentorRecurringSlotsAsync(mentorId);
            return Ok(new
            {
                success = true,
                message = "Lấy danh sách slots của mentor thành công",
                data = result
            });
        }

        [HttpPost]
        public async Task<IActionResult> AddMentorRecurringSlots([FromBody] Imate.API.Presentation.RequestModels.Mentors.AddMentorRecurringSlotsRequest request)
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { success = false, message = "Không tìm thấy thông tin người dùng" });
            }

            var result = await _mentorSlotService.AddMentorRecurringSlotsAsync(accountId, request.SlotIds);
            return Ok(new
            {
                success = true,
                message = "Thêm slots thành công",
                data = result
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMentorRecurringSlot(int id)
        {
            var accountIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(accountIdClaim, out int accountId))
            {
                return Unauthorized(new { success = false, message = "Không tìm thấy thông tin người dùng" });
            }

            var result = await _mentorSlotService.DeleteMentorRecurringSlotAsync(accountId, id);
            if (!result) return NotFound(new { success = false, message = "Không tìm thấy slot hoặc slot đã bị xóa" });

            return Ok(new
            {
                success = true,
                message = "Xóa slot thành công"
            });
        }
    }
}
