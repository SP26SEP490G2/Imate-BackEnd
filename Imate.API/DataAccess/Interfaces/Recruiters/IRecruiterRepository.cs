using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Recruiters
{
    public interface IRecruiterRepository : IRepositoryBase<Recruiter>
    {
        Task<Recruiter> GetRecruiterByIdAsync(int recruiterAccountId);
        Task<Recruiter> UpdateRecruiterAsync(Recruiter recruiter);
        IQueryable<Job> GetJobsByRecruiterId(int recruiterAccountId);
        Task<Job> CreateJobPostAsync(Job job);
        Task<Job> UpdateJobPostAsync(Job job);
        Task<Job> GetPostedJobByIdAsync(int jobId);
		IQueryable<JobApplication> GetJobApplicationsListByJobId(int jobId);
        IQueryable<Job> GetAllOpenJobs();
    }
}
