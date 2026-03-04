using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Enums;

namespace Imate.API.Business.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task AssignDefaultRoleAsync(int accountId, RoleName roleName)
        {
            var role = await _roleRepository.GetRoleByNameAsync(roleName);

            if (role == null)
            {
                throw new InvalidOperationException($"Role '{roleName}' không tồn tại trong hệ thống.");
            }

            await _roleRepository.AssignRoleToAccountAsync(accountId, role.Id);
        }

        public async Task UpdateUserRoleAsync(int accountId, RoleName newRoleName)
        {
            // Lấy role hiện tại của account
            var currentRoles = await _roleRepository.GetRolesByAccountIdAsync(accountId);
            var currentRole = currentRoles.FirstOrDefault();

            // Nếu không có role hiện tại, chỉ cần assign role mới
            if (currentRole == default(RoleName))
            {
                await AssignDefaultRoleAsync(accountId, newRoleName);
                return;
            }

            // Nếu role đã đúng, không cần update
            if (currentRole == newRoleName)
            {
                return;
            }

            // Lấy role cũ và role mới
            var oldRole = await _roleRepository.GetRoleByNameAsync(currentRole);
            var newRole = await _roleRepository.GetRoleByNameAsync(newRoleName);

            if (oldRole == null || newRole == null)
            {
                throw new InvalidOperationException("Không tìm thấy role trong hệ thống.");
            }

            // Xóa role cũ và thêm role mới
            await _roleRepository.RemoveRoleFromAccountAsync(accountId, oldRole.Id);
            await _roleRepository.AssignRoleToAccountAsync(accountId, newRole.Id);
        }

        public async Task<IEnumerable<RoleName>> GetRolesForAccountAsync(int accountId)
        {
            return await _roleRepository.GetRolesByAccountIdAsync(accountId);
        }
        //Test đến đây rồi
    }
}
