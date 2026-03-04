using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.UserManagement
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ImateDbContext _context;

        public RoleRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<Role> GetRoleByNameAsync(RoleName name)
        {
            var roleNameAsString = name.ToString();

            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task AssignRoleToAccountAsync(int accountId, int roleId)
        {
            // Kiểm tra xem AccountRole đã tồn tại chưa để tránh tracking conflict
            var existingAccountRole = await _context.AccountRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId);
            
            if (existingAccountRole != null)
            {
                // AccountRole đã tồn tại, không cần thêm lại
                return;
            }

            var accountRole = new AccountRole
            {
                AccountId = accountId,
                RoleId = roleId
            };

            _context.AccountRoles.Add(accountRole);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveRoleFromAccountAsync(int accountId, int roleId)
        {
            // Sử dụng AsNoTracking để tránh tracking conflict, sau đó attach entity để remove
            var accountRole = await _context.AccountRoles
                .AsNoTracking()
                .FirstOrDefaultAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId);
            
            if (accountRole != null)
            {
                // Attach entity với state Deleted để tránh tracking conflict
                _context.AccountRoles.Attach(accountRole);
                _context.AccountRoles.Remove(accountRole);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<RoleName>> GetRolesByAccountIdAsync(int accountId)
        {
            return await _context.AccountRoles
                .Where(ar => ar.AccountId == accountId)
                .Select(ar => ar.Role.Name) 
                .ToListAsync();
        }
    }
}
