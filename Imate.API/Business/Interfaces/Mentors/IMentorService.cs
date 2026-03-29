using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels;

using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels;

using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IMentorService
    {
        Task<PagedList<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync(CommonParams mentorParams);
        Task UpdateMentorProfileAsync(int accountId, UpdateMentorProfileRequest request);
        Task<CandidateRatingsResponseModel> GetCandidateRatingsAsync(int mentorAccountId);
        Task UpdateMentorPriceAsync(int accountId, int newPrice);
    }
}
