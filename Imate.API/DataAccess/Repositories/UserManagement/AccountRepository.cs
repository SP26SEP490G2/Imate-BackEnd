using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.UserManagement
{
    public class AccountRepository : IAccountRepository
    {
        public readonly ImateDbContext _context;
        public AccountRepository(ImateDbContext context)
        {
            _context = context;
        }
        public async Task<bool> AreUsersExisted(int id)
        {
            return await _context.Accounts.AnyAsync(a => a.Id == id);
        }
        public async Task<IEnumerable<Account>> GetAllAccountAsync()
        {
            // Eager loading roles để có dữ liệu đầy đủ
            return await _context.Accounts
                .Include(a => a.AccountRoles)
                    .ThenInclude(ar => ar.Role)
                .AsNoTracking() // Giúp tăng hiệu năng, vì chỉ đọc dữ liệu
                .ToListAsync();
        }
        public async Task<Account> GetByIdAsync(int id)
        {
            return await _context.Accounts.Include(a => a.AccountRoles).
                ThenInclude(ar => ar.Role)
                 .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Account> GetByProviderIdAsync(string providerId)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.ProviderId == providerId);
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            return await _context.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.Accounts.AnyAsync(a => a.Email == email);
        }

        public async Task AddAsync(Account account)
        {
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Account account)
        {
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();
        }
        public async Task<Account?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Accounts
                .Include(a => a.AccountRoles)
                    .ThenInclude(ar => ar.Role)
                .Include(a => a.Mentor)
                    .ThenInclude(m => m.MentorSkills)
                        .ThenInclude(ms => ms.Skill)
                .Include(a => a.Mentor)
                    .ThenInclude(m => m.MentorPositions)
                        .ThenInclude(mp => mp.Position)
                .Include(a => a.Mentor)
                    .ThenInclude(m => m.MentorCompanies)
                        .ThenInclude(mc => mc.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Account> GetByIdMentor(int id)
        {
            return await _context.Accounts
                .Include(a => a.Mentor)
                .Include(a => a.AccountRoles)
                    .ThenInclude(ar => ar.Role)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        public async Task<Account> GetByIdRecruiter(int id)
        {
            return await _context.Accounts
                .Include(a => a.Recruiter)
                .Include(a => a.AccountRoles)
                    .ThenInclude(ar => ar.Role)
                .FirstOrDefaultAsync(a => a.Id == id);
        }
        public IQueryable<Account> GetAllAccount()
        {
            return _context.Accounts.Include(a => a.AccountRoles)
                .ThenInclude(ar => ar.Role)
                .AsNoTracking();
        }
        public async Task DeleteAsync(Account account)
        {
            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
        }
    }
}
