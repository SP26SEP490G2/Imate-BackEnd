using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
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
            return  _context.Jobs
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

        public async Task<Job> CreateJobPostAsync(Job job)
        {
            _context.Jobs.Add(job);
            return job;
        }

        public async Task<Job> UpdateJobPostAsync(Job job)
        {
            _context.Jobs.Update(job);
            return job;
        }

        public async Task<Job> GetPostedJobByIdAsync(int jobId)
        {
            return await _context.Jobs
                .Include(j => j.JobSkills)
                    .ThenInclude(s=>s.Skill)
                .Include(j => j.JobPositions)
                    .ThenInclude(s => s.Position)
                .Include(j => j.JobApplications)
                .FirstOrDefaultAsync(j=>j.Id == jobId);

        }

		public IQueryable<JobApplication> GetJobApplicationsListByJobId(int jobId)
		{
			return  _context.JobApplications
                .Include(a=>a.Candidate)
                .Include (b=>b.Cv)
                .Where(j=>j.Job.Id == jobId)
				.AsNoTracking();
		}

		public IQueryable<Job> GetAllOpenJobs()
		{
            return _context.Jobs
                .Include(j => j.JobSkills)
                .Include(j => j.JobPositions)
                .Include(j => j.Recruiter)
                .ThenInclude(j=>j.Recruiter)
                .AsNoTracking();
		}

		public async Task<JobApplication> UpdateJobApplicationStatusAsync(JobApplication jobApplication)
		{
			_context.JobApplications.Update(jobApplication);
			return jobApplication;
		}

		public async Task<JobApplication> GetJobApplicationByIdAsync(int jobApplicationId)
		{
			return await _context.JobApplications
                .Include(a => a.Candidate)
                .Include(a=>a.Job)
				.FirstOrDefaultAsync(j=>j.Id==jobApplicationId);
		}

		public IQueryable<JobApplication> GetCandidateAppliedJob(int candidateId)
		{
			return _context.JobApplications
				.Include(j => j.Job)
				.Include(ja => ja.Candidate)
				.Where(j => j.CandidateId == candidateId)
				.AsNoTracking();
		}

		public async Task<JobApplication> CreateJobApplicationAsync(JobApplication jobApplication)
		{
			_context.JobApplications.Add(jobApplication);
            return jobApplication;
		}

		public IQueryable<JobApplication> GetAllJobApplication()
		{
			return _context.JobApplications.AsNoTracking();
		}

		public async Task<List<Job>> GetJobsToCloseAsync()
		{
			return await _context.Jobs
				.Where(j => j.ApplicationDeadline < DateTime.UtcNow
						 && j.Status != JobStatus.Closed)
				.ToListAsync();
		}
	}
}
