using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Repositories.Mentors;
using Imate.API.DataAccess.Repositories.QuestionBank;
using Imate.API.DataAccess.Repositories.UserManagement;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Repositories.Recruiters;

namespace Imate.API.DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
        public UnitOfWork(ImateDbContext repositoryContext, IAccountRepository accounts, IMentorRepository mentors, IRecruiterRepository recruiters, ICategoryRepository categories, IQuestionRepository questions, ISkillRepository skills)
        {
            _repositoryContext = repositoryContext;
            Accounts = accounts;
            Mentors = mentors;
            Recruiters = recruiters;
            Categories = categories;
            Questions = questions;
            Skills = skills;
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
