using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IMentorService
    {
        Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync();
        Task UpdateMentorProfileAsync(int accountId, UpdateMentorProfileRequest request);

    }
}
