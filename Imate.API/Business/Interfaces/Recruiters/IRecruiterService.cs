using Imate.API.Presentation.RequestModels.Recruiters;

namespace Imate.API.Business.Interfaces.Recruiters
{
    public interface IRecruiterService
    {
        Task SubmitRecruiterProfileAsync(int accountId, SubmitRecruiterProfileRequest request);
    }
}
