using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Repositories
{
    public class AccountRepository : RepositoryBase<Account>, IAccountRepository
    {
        public AccountRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
        private IAccountRepository? _accountRepository;

        public UnitOfWork(ImateDbContext repositoryContext)
        {
            _repositoryContext = repositoryContext;
        }

        public IAccountRepository Account
        {
            get
            {
                if (_accountRepository == null)
                    _accountRepository = new AccountRepository(_repositoryContext);

                return _accountRepository;
            }
        }

        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
    }
}
