using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Repositories;
using Imate.API.DataAccess.Repositories.Mentors;
using Imate.API.DataAccess.Repositories.QuestionBank;
using Imate.API.DataAccess.Repositories.UserManagement;

namespace Imate.API.DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
        public UnitOfWork(ImateDbContext repositoryContext, IAccountRepository accounts, IMentorRepository mentors, IRecruiterRepository recruiter)
        {
            _repositoryContext = repositoryContext;
            Accounts = accounts;
            Mentors = mentors;
            Recruiters = recruiter;
        }
        public IUserSubscriptionRepository UserSubscriptions { get; private set; }
        public IBookingRepository Bookings { get; private set; }
        public IQuestionRepository Questions { get; private set; }
        public IMentorRepository Mentors { get; private set; }
        public IAccountRepository Accounts { get; private set; }
        public ICategoryRepository Categories { get; private set; }
        public IPositionRepository Positions { get; private set; }
        public ISkillRepository Skills { get; private set; }
        public IRecruiterRepository Recruiters { get; private set; }
        public Task SaveChangesAsync() => _repositoryContext.SaveChangesAsync();
        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
    }
}
