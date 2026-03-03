using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Interfaces
{
    public interface IMentorService
    {
        Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentors();
    }
}
