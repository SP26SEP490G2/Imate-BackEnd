using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.Notification;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.Notification
{
    public class SystemNotificationRepository : ISystemNotificationRepository
    {
        private readonly ImateDbContext _context;

        public SystemNotificationRepository(ImateDbContext context)
        {
            _context = context;
        }
        public async Task AddAsync(SystemNotification notification)
        {
            await _context.SystemNotifications.AddAsync(notification);
        }
        public async Task<SystemNotification?> GetByIdAsync(int id)
        {
            return await _context.SystemNotifications.FindAsync(id);
        }
        public async Task<IEnumerable<SystemNotification>> GetForUserAsync(int recipientUserId, int pageIndex, int pageSize)
        {
            return await _context.SystemNotifications
                .Where(n => n.RecipientUserId == recipientUserId)
                .OrderByDescending(n => n.CreatedAt) // Mới nhất lên đầu
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .AsNoTracking() // Tối ưu hiệu suất cho truy vấn chỉ đọc
                .ToListAsync();
        }
        public async Task<int> GetUnreadCountAsync(int recipientUserId)
        {
            return await _context.SystemNotifications
                .CountAsync(n => n.RecipientUserId == recipientUserId && !n.IsRead);
        }
        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _context.SystemNotifications.FindAsync(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                _context.SystemNotifications.Update(notification);
            }
        }
        public async Task<int> MarkAllAsReadAsync(int recipientUserId)
        {
            return await _context.SystemNotifications
                .Where(n => n.RecipientUserId == recipientUserId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }
        public async Task DeleteAsync(int notificationId)
        {
            var notification = await _context.SystemNotifications.FindAsync(notificationId);
            if (notification != null)
            {
                _context.SystemNotifications.Remove(notification);
            }
        }
        public async Task<IEnumerable<SystemNotification>> GetForStaffAsync(string recipientRole, int pageIndex, int pageSize)
        {
            if (!Enum.TryParse<Imate.API.Models.Enums.RoleName>(recipientRole, true, out var roleEnum))
            {
                return new List<SystemNotification>();
            }

            return await _context.SystemNotifications
                .Where(n => n.RecipientUser != null && n.RecipientUser.AccountRoles.Any(ar => ar.Role.Name == roleEnum))
                .OrderByDescending(n => n.CreatedAt) // Mới nhất lên đầu
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .AsNoTracking() // Tối ưu hiệu suất cho truy vấn chỉ đọc
                .ToListAsync();
        }
    }
}
