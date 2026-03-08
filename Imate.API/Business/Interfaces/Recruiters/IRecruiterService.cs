using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.ResponseModels.Recruiter;

namespace Imate.API.Business.Interfaces.Recruiters
{
    public interface IRecruiterService
    {
        Task UpdataRecruiterrProfileAsync(int accountId, UpdateRecruiterProfileRequest request);
        Task SubmitRecruiterProfileAsync(int accountId, SubmitRecruiterProfileRequest request);
        Task<IEnumerable<GetJobRecruiterResponse>> GetListJobRecruiterAsync(int accountId, RecruiterJobSearchFilterRequest filterRequest);

    }
}
