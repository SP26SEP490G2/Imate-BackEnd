using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.UserManagement
{
    public interface IUserCvRepository
    {
        Task<UserCv?> GetByIdAsync(int id);
        Task<UserCv?> GetByIdWithUserAsync(int id);
        Task<IEnumerable<UserCv>> GetByAccountIdAsync(int accountId);
        Task<bool> FileNameExistsForUserAsync(int accountId, string fileName);
        Task AddAsync(UserCv userCv);
        Task UpdateAsync(UserCv userCv);
        Task DeleteAsync(UserCv userCv);
        Task<UserCv?> GetByAccountIdAndFileNameAsync(int accountId, string fileName);
    }
}

