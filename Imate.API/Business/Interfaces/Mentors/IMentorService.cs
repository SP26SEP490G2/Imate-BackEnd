using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Interfaces.Mentors
{
    public interface IMentorService
    {
        Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync();
    }
}
