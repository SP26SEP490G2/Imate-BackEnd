using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Repositories.UserManagement;
using Imate.API.DataAccess.Repositories;
using Imate.API.DataAccess.Repositories.Mentors;
using Imate.API.DataAccess.Repositories.QuestionBank;

namespace Imate.API.DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
        public UnitOfWork(ImateDbContext repositoryContext, IAccountRepository accounts)
        {
            _repositoryContext = repositoryContext;
            Accounts = accounts;
        }
        public IUserSubscriptionRepository UserSubscriptions { get; private set; }
        public IBookingRepository Bookings { get; private set; }
        public IQuestionRepository Questions { get; private set; }
        public IMentorRepository Mentors { get; private set; }
        public IAccountRepository Accounts { get; private set; }

        public IQuestionCategoryRepository QuestionCategories { get; private set; }

        public Task SaveChangesAsync() => _repositoryContext.SaveChangesAsync();
        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
    }
}
