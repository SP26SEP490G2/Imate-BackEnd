using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.UserManagement
{
    public class UserCvRepository : IUserCvRepository
    {
        private readonly ImateDbContext _context;

        public UserCvRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<UserCv?> GetByIdAsync(int id)
        {
            return await _context.UserCvs
                .FirstOrDefaultAsync(cv => cv.Id == id);
        }

        public async Task<UserCv?> GetByIdWithUserAsync(int id)
        {
            return await _context.UserCvs
                .Include(cv => cv.Account)
                .FirstOrDefaultAsync(cv => cv.Id == id);
        }

        public async Task<IEnumerable<UserCv>> GetByAccountIdAsync(int accountId)
        {
            return await _context.UserCvs
                .Where(cv => cv.AccountId == accountId)
                .OrderByDescending(cv => cv.UploadDate)
                .ToListAsync();
        }

        public async Task<bool> FileNameExistsForUserAsync(int accountId, string fileName)
        {
            return await _context.UserCvs
                .AnyAsync(cv => cv.AccountId == accountId && cv.FileName.ToLower() == fileName.ToLower());
        }

        public async Task AddAsync(UserCv userCv)
        {
            _context.UserCvs.Add(userCv);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(UserCv userCv)
        {
            _context.UserCvs.Update(userCv);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(UserCv userCv)
        {
            _context.UserCvs.Remove(userCv);
            await _context.SaveChangesAsync();
        }

        public async Task<UserCv?> GetByAccountIdAndFileNameAsync(int accountId, string fileName)
        {
            return await _context.UserCvs
                .FirstOrDefaultAsync(cv => cv.AccountId == accountId && cv.FileName.ToLower() == fileName.ToLower());
        }
    }
}

