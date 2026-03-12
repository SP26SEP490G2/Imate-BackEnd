using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.DataAccess.Interfaces.Mentors
{
    public interface IMentorRepository : IRepositoryBase<Mentor>
    {
        Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync();
        Task<Mentor?> GetMentorByIdAsync(int id);
        Task<Mentor?> GetByIdAsync(int id);
        Task<Mentor> UpdateMentorAsync(Mentor mentor);
    }
}
