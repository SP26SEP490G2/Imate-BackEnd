using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Notification
{
    public interface ISystemNotificationRepository
    {

        Task AddAsync(SystemNotification notification);
        Task<SystemNotification?> GetByIdAsync(int id);
        Task<IEnumerable<SystemNotification>> GetForUserAsync(int recipientUserId, int pageIndex, int pageSize);
        Task<int> GetUnreadCountAsync(int recipientUserId);
        Task MarkAsReadAsync(int notificationId);
        Task<int> MarkAllAsReadAsync(int recipientUserId);
        Task DeleteAsync(int notificationId);
        Task<IEnumerable<SystemNotification>> GetForStaffAsync(string recipientRole, int pageIndex, int pageSize);


    }
}
