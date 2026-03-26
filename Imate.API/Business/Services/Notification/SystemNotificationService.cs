using Microsoft.AspNetCore.SignalR;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.SignalR;

namespace Imate.API.Business.Services.Notification
{
    public class SystemNotificationService : ISystemNotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<SystemNotificationHub> _hubContext;
        private readonly ISystemConfigService _systemConfigService;

        public SystemNotificationService(
            IUnitOfWork unitOfWork,
            IHubContext<SystemNotificationHub> hubContext,
            ISystemConfigService systemConfigService)
        {
            _unitOfWork = unitOfWork;
            _hubContext = hubContext;
            _systemConfigService = systemConfigService;
        }
        // 1. HÀM TẠO VÀ GỬI THÔNG BÁO (nhận int userId)
        public async Task CreateAndSendNotificationAsync(int userId, string message, string link)
        {
            var notification = new SystemNotification // Dùng entity của bạn
            {
                RecipientUserId = userId, // Dùng trực tiếp
                Message = message,
                Link = link,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.SystemNotifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            var notificationPayload = new
            {
                Id = notification.Id.ToString(), // Vẫn gửi string Id cho React
                Message = notification.Message,
                Link = notification.Link,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            };

            // **QUAN TRỌNG**
            // Hub Context *vẫn* map user bằng string (từ JWT)
            // nên chúng ta phải chuyển int userId ngược lại string
            await _hubContext.Clients
                .User(userId.ToString()) // Gửi cho string UserId (của SignalR connection)
                .SendAsync("ReceiveNotification", notificationPayload);
        }

        // 2. HÀM LẤY THÔNG BÁO CŨ (nhận int userId)
        public async Task<IEnumerable<object>> GetNotificationsForUserAsync(int userId)
        {
            var pageSize = await _systemConfigService.GetNotificationPageSizeAsync();
            var user = await _unitOfWork.Accounts.GetByIdAsync(userId);
            var roleName = user.AccountRoles.FirstOrDefault()?.Role.Name;
            var notifications = await _unitOfWork.SystemNotifications
                               .GetForStaffAsync(roleName.ToString(), 0, pageSize); // Mặc định
            if (user.AccountRoles.Any(a => a.Role.Name != RoleName.Staff))
            {
                notifications = await _unitOfWork.SystemNotifications
                               .GetForUserAsync(userId, 0, pageSize); // Dùng config
            }


            // Chuyển đổi kết quả (giữ nguyên)
            return notifications.Select(n => new
            {
                Id = n.Id.ToString(),
                Message = n.Message,
                Link = n.Link,
                Type = n.Type,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            });
        }

        // 3. HÀM ĐÁNH DẤU ĐÃ ĐỌC (nhận int userId, int notificationId)
        public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
        {
            // Bỏ 2 hàm TryParse

            // 1. Lấy thông báo
            var notification = await _unitOfWork.SystemNotifications.GetByIdAsync(notificationId);

            if (notification.IsRead)
            {
                return true;
            }

            // 4. Gọi Repository
            await _unitOfWork.SystemNotifications.MarkAsReadAsync(notificationId);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }


        // 4. HÀM ĐẾM (nhận int userId)
        public async Task<int> GetUnreadNotificationCountAsync(int userId)
        {
            return await _unitOfWork.SystemNotifications.GetUnreadCountAsync(userId);
        }
        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            // 1. Gọi Repository (Repository của bạn đã có hàm này)
            // (Nó sẽ cập nhật tất cả các bản ghi có (userId) và (IsRead == false) sang (IsRead == true))
            int rowsAffected = await _unitOfWork.SystemNotifications.MarkAllAsReadAsync(userId);

            // 2. Lưu thay đổi
            if (rowsAffected > 0)
            {
                await _unitOfWork.SaveChangesAsync();
                return true;
            }

            return false;
        }
    }

}

