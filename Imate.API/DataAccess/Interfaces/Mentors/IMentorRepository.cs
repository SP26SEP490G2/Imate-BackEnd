using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.DataAccess.Interfaces.Mentors
{
    public interface IMentorRepository : IRepositoryBase<Mentor>
    {
        Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync();
    }
}
