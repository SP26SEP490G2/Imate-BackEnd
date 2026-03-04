using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Repositories.UserManagement;
using Imate.API.DataAccess.Repositories;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.Interfaces;

namespace Imate.API.DataAccess.Repositories
{
    public class AccountRepository : RepositoryBase<Account>, IAccountRepository2
    {
        public AccountRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
    public class MentorRepository : RepositoryBase<Mentor>, IMentorRepository
    {
        public MentorRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
        public UnitOfWork(ImateDbContext repositoryContext)
        {
            _repositoryContext = repositoryContext;
        }
        private IAccountRepository2? _accountRepository;
        public IUserSubscriptionRepository UserSubscriptions { get; private set; }
        public IBookingRepository Bookings { get; }
        public IQuestionRepository Questions { get; private set; }
        private IMentorRepository? _mentorRepository;
        public IAccountRepository Accounts { get; private set; }
        public IAccountRepository2 Account
        {
            get
            {
                if (_accountRepository == null)
                    _accountRepository = new AccountRepository(_repositoryContext);

                return _accountRepository;
            }
        }
        public IMentorRepository Mentor
        {
            get
            {
                if (_mentorRepository == null)
                    _mentorRepository = new MentorRepository(_repositoryContext);

                return _mentorRepository;
            }
        }


        public Task SaveChangesAsync() => _repositoryContext.SaveChangesAsync();
        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
    }
}
