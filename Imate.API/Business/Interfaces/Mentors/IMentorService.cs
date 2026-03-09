using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IMentorService
    {
        Task<PagedList<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync(CommonParams mentorParams);
        Task UpdateMentorProfileAsync(int accountId, UpdateMentorProfileRequest request);

    }
}
