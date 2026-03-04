using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.Repositories;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IAccountRepository2 : IRepositoryBase<Account>
    {
        // Add specific methods here
    }
    public interface IMentorRepository : IRepositoryBase<Mentor>
    {
        // Add specific methods here
    }


    public interface IUnitOfWork
    {

        IAccountRepository Accounts { get; }
        IAccountRepository2 Account { get; }
        IBookingRepository Bookings { get; }
        IUserSubscriptionRepository UserSubscriptions { get; }
        IQuestionRepository Questions { get; }
        Task SaveChangesAsync();
        Task SaveAsync();
    }
}
