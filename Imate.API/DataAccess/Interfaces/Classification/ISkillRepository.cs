using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Classification
{
    public interface ISkillRepository
    {
        Task<List<int>> GetNonExistingSkillIdsAsync(IEnumerable<int> categoryIds);
        Task<List<string>> GetNonExistingSkillNames(IEnumerable<string> skillNames);
        Task<List<Skill>> FindSkillsByNamesAsync(IEnumerable<string> names);
        IQueryable<Skill> GetAllSkills();
        Task<Skill> GetSkillByIdAsync(int id);
        Task<Skill> AddSkillAsync(Skill Skill);
        Task<Skill> UpdateSkillAsync(Skill Skill);
        void SaveChangeAsync();

    }
}
