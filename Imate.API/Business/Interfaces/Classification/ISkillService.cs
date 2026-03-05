using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;

namespace Imate.API.Business.Interfaces.Classification
{
    public interface ISkillService
    {
        Task<List<int>> GetNonExistingSkillIdsAsync(IEnumerable<int> categoryIds);
        Task<PagedList<SkillResponse>> GetAllSkillsAsync(CommonParams skillParams);
        Task<Skill> UpdateSkillsAsync(int id, SkillUpdateRequest category);
        Task<Skill> AddSkillsAsync(SkillCreateRequest category);
        Task<List<AffectedQuestionResponseModel>> GetAffectedQuestionsAsync(int categoryId, bool willBeActive);
        Task<Skill?> SetSkillStatusAsync(int id, bool isActive);
    }
}
