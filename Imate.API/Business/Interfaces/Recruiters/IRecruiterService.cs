using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.Presentation.RequestModels.UserManagement;

namespace Imate.API.Business.Interfaces.Recruiters
{
    public interface IRecruiterService
    {
        Task UpdataRecruiterrProfileAsync(int accountId, UpdateRecruiterProfileRequest request);
        Task SubmitRecruiterProfileAsync(int accountId, SubmitRecruiterProfileRequest request);
    }
}
