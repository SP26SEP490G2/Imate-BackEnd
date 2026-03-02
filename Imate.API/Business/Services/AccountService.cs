using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;

namespace Imate.API.Business.Services
{
    public class AccountService : IAccountService
    {
        private readonly IUnitOfWork _repository;

        public AccountService(IUnitOfWork repository)
        {
            _repository = repository;
        }

        public IEnumerable<Account> GetAllAccounts()
        {
            return _repository.Account.FindAll(trackChanges: false).ToList();
        }

        public Account? GetAccountById(int id)
        {
            return _repository.Account.FindByCondition(a => a.Id.Equals(id), trackChanges: false).SingleOrDefault();
        }

        public void CreateAccount(Account account)
        {
            _repository.Account.Create(account);
            _repository.SaveAsync().Wait(); // Or make async
        }
    }
}
