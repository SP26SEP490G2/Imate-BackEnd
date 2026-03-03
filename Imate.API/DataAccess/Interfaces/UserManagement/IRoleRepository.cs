using Imate.API.Models.Entities;
using Imate.API.Models.Enums;

namespace Imate.API.DataAccess.Interfaces.UserManagement
{
    public interface IRoleRepository
    {
        Task<Role> GetRoleByNameAsync(RoleName name);

        Task AssignRoleToAccountAsync(int accountId, int roleId);

        Task RemoveRoleFromAccountAsync(int accountId, int roleId);

        Task<IEnumerable<RoleName>> GetRolesByAccountIdAsync(int accountId);
    }
}
