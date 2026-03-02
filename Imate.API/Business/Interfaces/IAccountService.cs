using Imate.API.Models.Entities;

namespace Imate.API.Business.Interfaces
{
    public interface IAccountService
    {
        IEnumerable<Account> GetAllAccounts();
        Account? GetAccountById(int id);
        void CreateAccount(Account account);
        // Add other methods
    }
}
