namespace Imate.API.Business.Interfaces.Notification
{
    public interface ISystemNotificationService
    {
        // Đã đổi "string userId" -> "int userId"
        Task<IEnumerable<object>> GetNotificationsForUserAsync(int userId);

        // Đã đổi "string userId" -> "int userId"
        // Đã đổi "string notificationId" -> "int notificationId"
        Task<bool> MarkAsReadAsync(int userId, int notificationId);

        // Đã đổi "string userId" -> "int userId"
        Task CreateAndSendNotificationAsync(int userId, string message, string link);

        // Đã đổi "string userId" -> "int userId"
        Task<int> GetUnreadNotificationCountAsync(int userId);
        Task<bool> MarkAllAsReadAsync(int userId);

    }
}
