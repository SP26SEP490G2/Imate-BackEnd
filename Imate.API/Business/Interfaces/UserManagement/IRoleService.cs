using Imate.API.Models.Enums;

namespace Imate.API.Business.Interfaces.UserManagement
{
    public interface IRoleService
    {
        Task AssignDefaultRoleAsync(int accountId, RoleName roleName);
        Task UpdateUserRoleAsync(int accountId, RoleName newRoleName);
        Task<IEnumerable<RoleName>> GetRolesForAccountAsync(int accountId);
    }
}
