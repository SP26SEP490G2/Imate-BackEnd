using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Imate.API.Business.Interfaces.Notification;
using System.Security.Claims;

namespace Imate.API.Presentation.SignalR
{
    [Authorize]
    public class SystemNotificationHub : Hub
    {
        private readonly ISystemNotificationService _notificationService;
        public SystemNotificationHub(ISystemNotificationService notificationService)
        {
            _notificationService = notificationService;
        }
        // HÀM HELPER: Lấy int User ID từ Context
        // React đang gọi hàm này: "MarkNotificationAsRead"
        public async Task MarkNotificationAsRead(string notificationId)
        {
            // 1. Logic chuyển đổi ID nằm trong Hub
            if (!TryGetUserId(out int userId))
            {
                throw new HubException("User is not authenticated or has invalid ID.");
            }

            if (!int.TryParse(notificationId, out int intNotificationId))
            {
                throw new HubException("Invalid notification ID format.");
            }

            // 2. Service được gọi với "int userId" và "int intNotificationId" sạch sẽ
            await _notificationService.MarkAsReadAsync(userId, intNotificationId);

            // (Tùy chọn) Gửi tín hiệu lại cho client để UI cập nhật
            await Clients.Caller.SendAsync("NotificationMarkedAsRead", notificationId);
        }

        // --- HÀM 2 (React gọi): Khi click "Đánh dấu tất cả đã đọc" ---
        // (Bạn cần thêm "MarkAllAsReadAsync" vào ISystemNotificationService)
        public async Task MarkAllNotificationsAsRead()
        {
            if (!TryGetUserId(out int userId))
            {
                throw new HubException("User is not authenticated.");
            }

            // (Giả sử ISystemNotificationService của bạn có hàm này)
            // await _notificationService.MarkAllAsReadAsync(userId);
            await _notificationService.MarkAllAsReadAsync(userId);
            // Gửi tín hiệu lại cho client
            await Clients.Caller.SendAsync("AllNotificationsMarkedAsRead");
        }

        // Tự động gọi khi Client kết nối thành công
        public override async Task OnConnectedAsync()
        {

            // 1. Lấy ID (như cũ)
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            // 2. LẤY ROLE (VAI TRÒ) (MỚI)
            var userRole = Context.User.FindFirstValue(ClaimTypes.Role);
            // LOGGING DEBUG
            Console.WriteLine($"--- SIGNALR CONNECTED ---");
            Console.WriteLine($"Connection ID: {Context.ConnectionId}");
            Console.WriteLine($"User ID (Claim): {userId ?? "NULL"}");
            Console.WriteLine($"User Role (Claim): {userRole ?? "NULL"}"); // Quan trọng!!!
            if (!string.IsNullOrEmpty(userId))
            {
                // 3. THÊM VÀO NHÓM CÁ NHÂN (như cũ)
                await Groups.AddToGroupAsync(Context.ConnectionId, userId);
            }
            if (!string.IsNullOrEmpty(userRole))
            {
                // 4. THÊM VÀO NHÓM THEO VAI TRÒ (MỚI)
                // (Ví dụ: "Staff", "Admin", "Candidate")
                await Groups.AddToGroupAsync(Context.ConnectionId, userRole);
            }

            await base.OnConnectedAsync();
        }

        // Tự động gọi khi Client ngắt kết nối
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userRole = Context.User.FindFirstValue(ClaimTypes.Role);

            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            }
            if (!string.IsNullOrEmpty(userRole))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, userRole);
            }

            await base.OnDisconnectedAsync(exception);
        }
        private bool TryGetUserId(out int userId)
        {
            // Context.UserIdentifier là string ID (ví dụ: "123")
            var stringUserId = Context.UserIdentifier;

            // Chuyển đổi ID
            if (int.TryParse(stringUserId, out userId))
            {
                return true;
            }

            userId = 0;
            return false;
        }
    }
}

