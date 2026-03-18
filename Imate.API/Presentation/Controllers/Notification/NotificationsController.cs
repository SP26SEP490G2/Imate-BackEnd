using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.DataAccess.Interfaces;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Notification
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ISystemNotificationService _notificationService;
        public NotificationsController(ISystemNotificationService notificationService)
        {
            _notificationService = notificationService;
        }
        // HÀM HELPER: Lấy int User ID từ Claims
        private bool TryGetUserId(out int userId)
        {
            // Lấy string ID (từ JWT, ví dụ: "123")
            var stringUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Chuyển đổi ID
            if (int.TryParse(stringUserId, out userId))
            {
                return true;
            }

            userId = 0;
            return false;
        }

        // GET /api/notifications/my-notifications (React đang gọi cái này)
        [HttpGet("my-notifications")]
        public async Task<IActionResult> GetMyNotifications()
        {
            // 1. Logic chuyển đổi ID nằm trong Controller
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized("Invalid user ID format.");
            }

            // 2. Service được gọi với "int userId" sạch sẽ
            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);

            return Ok(notifications);
        }

    }
}
