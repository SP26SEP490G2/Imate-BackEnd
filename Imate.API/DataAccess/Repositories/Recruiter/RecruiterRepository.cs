using Imate.API.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories
{
    public class RecruiterRepository : IRecruiterRepository
    {
        private readonly ImateDbContext _context;
        public RecruiterRepository(ImateDbContext repositoryContext)
        {
            _context = repositoryContext;
        }
        public async Task<Recruiter> GetRecruiterByIdAsync(int id)
        {
            var recruiter = await _context.Recruiters.
                Include(m => m.Account)
                .Where(Recruiter => Recruiter.AccountId == id).
                FirstOrDefaultAsync(m => m.AccountId == id);
            return recruiter;
        }
        public async Task<Recruiter> UpdateRecruiterAsync(Recruiter recruiter)
        {
            _context.Recruiters.Update(recruiter);
            await _context.SaveChangesAsync();
            return recruiter;
        }
    }
}
