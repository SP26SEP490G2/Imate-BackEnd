using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Recruiters
{
    public class RecruiterRepository : RepositoryBase<Recruiter>, IRecruiterRepository
    {
        private readonly ImateDbContext _context;

        public RecruiterRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
            _context = repositoryContext;

        }

        public IQueryable<Job> GetJobsByRecruiterId(int recruiterAccountId)
        {
            return _context.Jobs
                .Include(j => j.JobSkills)
                .Include(j => j.JobPositions)
                .Include(j => j.JobApplications)
                .Include(j => j.Recruiter)
                .Where(j => j.RecruiterId == recruiterAccountId)
                .AsNoTracking();
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
