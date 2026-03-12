using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.UserManagement
{
    public interface IAccountRepository
    {
        Task<bool> AreUsersExisted(int id);
        Task<IEnumerable<Account>> GetAllAccountAsync();
        Task<Account> GetByIdAsync(int id);

        Task<Account> GetByProviderIdAsync(string providerId);
        Task<Account?> GetByEmailAsync(string email);

        Task<bool> ExistsByEmailAsync(string email);
        Task AddAsync(Account account);
        Task UpdateAsync(Account account);
        Task DeleteAsync(Account account);
        Task<Account?> GetByIdWithDetailsAsync(int id);
        Task<Account?> GetByIdMentorWithDetailsAsync(int id);
        Task<Account?> GetByIdForStatusUpdateAsync(int id);
        Task<Account?> GetByIdMentor(int id);
        Task<Account?> GetByIdRecruiter(int id);
        Task<IEnumerable<Account>> GetPendingMentorAccountsAsync();
        Task<(IEnumerable<Account> Items, int TotalCount)> GetPendingMentorAccountsPagedAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<IEnumerable<Account>> GetPendingRecruiterAccountsAsync();
        IQueryable<Account> GetAllAccount();




    }
}
