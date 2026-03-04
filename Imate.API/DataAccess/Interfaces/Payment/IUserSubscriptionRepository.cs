using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Payment
{
    public interface IUserSubscriptionRepository
    {
        IQueryable<UserSubscription> GetUserSubscriptions();
        Task<List<UserSubscription>> GetSubscriptionsByCandidateIdAsync(int candidateId);
        Task<UserSubscription> GetActiveSubscriptionByCandidateIdAsync(int id);
        void AddUserSubscription(UserSubscription userSubscription);
        Task IncrementMockInterviewUsedAsync(int subscriptionId);
        Task<int> CountInterviewsTodayAsync(int candidateId, DateTime date);
    }
}
