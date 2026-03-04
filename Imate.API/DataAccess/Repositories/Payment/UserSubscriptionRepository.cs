using Microsoft.EntityFrameworkCore;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.Payment
{
    public class UserSubscriptionRepository : IUserSubscriptionRepository

    {
        private readonly ImateDbContext _context;
        public UserSubscriptionRepository(ImateDbContext context)
        {
            _context = context;
        }
        public IQueryable<UserSubscription> GetUserSubscriptions()
        {
            return _context.UserSubscriptions.
                Include(a => a.Package).AsQueryable();
        }
        public async Task<List<UserSubscription>> GetSubscriptionsByCandidateIdAsync(int candidateId)
        {
            // Cần Include(us => us.Package) để lấy được Tên và DurationDays
            return await _context.UserSubscriptions
                .Include(us => us.Package)
                .Where(us => us.CandidateId == candidateId)
                .OrderByDescending(us => us.StartDate) // Lấy gói mới nhất lên đầu
                .ToListAsync();
        }

        public async Task<UserSubscription> GetActiveSubscriptionByCandidateIdAsync(int id)
        {
            var now = DateTime.UtcNow;
            var subscriptions = await _context.UserSubscriptions
                .Include(us => us.Package)
                .Where(us => us.CandidateId == id && us.IsActive == true)
                .ToListAsync();

            // Kiểm tra EndDateTime từ CreatedAt + DurationDays thay vì EndDate
            return subscriptions.FirstOrDefault(us =>
            {
                if (us.Package == null) return false;
                
                // Nếu không có DurationDays, subscription không giới hạn thời gian
                if (!us.Package.DurationDays.HasValue || us.Package.DurationDays.Value <= 0)
                    return true;
                
                // Tính EndDateTime từ CreatedAt + DurationDays
                var endDateTime = us.CreatedAt.AddDays(us.Package.DurationDays.Value);
                return endDateTime > now;
            });
        }
        public void AddUserSubscription(UserSubscription userSubscription)
        {
            _context.UserSubscriptions.Add(userSubscription);
        }

        public async Task IncrementMockInterviewUsedAsync(int subscriptionId)
        {
            var subscription = await _context.UserSubscriptions
                .FirstOrDefaultAsync(us => us.Id == subscriptionId);
            
            if (subscription != null)
            {
                subscription.MockInterviewUsed++;
                _context.UserSubscriptions.Update(subscription);
            }
        }

        public async Task<int> CountInterviewsTodayAsync(int candidateId, DateTime date)
        {
            // Chuyển đổi date sang UTC để so sánh
            var dateStart = date.Date.ToUniversalTime();
            var dateEnd = dateStart.AddDays(1);

            // Đếm số interview session được tạo trong ngày
            // Bao gồm: CvBased, FullSession (full interview), và Single_Question (practice)
            return await _context.InterviewSessions
                .Where(s => s.AccountId == candidateId &&
                           s.StartTime >= dateStart &&
                           s.StartTime < dateEnd &&
                           (s.InterviewType == InterviewType.CvBased || 
                            s.InterviewType == InterviewType.FullSession || 
                            s.InterviewType == InterviewType.SingleQuestion))
                .CountAsync();
        }
    }
}
