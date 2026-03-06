using Imate.API.Presentation.RequestModels.UserManagement;

namespace Imate.API.Business.Interfaces.Recruiter
{
    public interface IRecruiterService
    {
        Task UpdataRecruiterrProfileAsync(int accountId, UpdateRecruiterProfileRequest request);
    }
}
