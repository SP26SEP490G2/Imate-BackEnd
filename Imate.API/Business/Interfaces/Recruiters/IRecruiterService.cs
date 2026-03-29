using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.JobApplications;
using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.ResponseModels.JobApplications;
using Imate.API.Presentation.ResponseModels.Recruiter;
using Microsoft.Identity.Client;
using Microsoft.AspNetCore.Http;

namespace Imate.API.Business.Interfaces.Recruiters
{
    public interface IRecruiterService
    {
        Task UpdataRecruiterrProfileAsync(int accountId, UpdateRecruiterProfileRequest request);
        Task SubmitRecruiterProfileAsync(int accountId, SubmitRecruiterProfileRequest request);
        Task<PagedList<GetJobRecruiterResponse>> GetListJobRecruiterAsync(int accountId, RecruiterJobSearchFilterRequest filterRequest);
        Task<Job> CreateJobPostAsync(int accountId, CreateUpdateJobRequest request);
        Task<Job> UpdateJobPostAsync(int accountId, CreateUpdateJobRequest request);

        Task<Job> CloseJobPostAsync(int accountId, int jobId);
        Task<PagedList<GetAppliedJobApplicationCandidateResponse>> GetAppliedCandidateByJobIdAsync(int jobId, AppliedApplicationCandidateFilterRequest filterRequest);
        Task<PagedList<GetAllOpenedJobResponse>> GetAllOpenedJobs(JobPostingCandidateFilter filterRequest);
        Task<GetAllOpenedJobResponse> GetJobDetail(int jobId);
        Task<JobApplication> UpdateJobApplication(int accountId, UpdateJobApplicationRequest request);
        Task<PagedList<GetCandidateAppliedJobResponse>> GetCandidateAppliedJob(int accountId, AppliedApplicationCandidateFilterRequest request);

        Task<JobApplication> CreateJobApplication(int accountId, CreateJobApplicationRequest request);
        Task<string> UploadCompanyLogoAsync(IFormFile file);
	}
}
