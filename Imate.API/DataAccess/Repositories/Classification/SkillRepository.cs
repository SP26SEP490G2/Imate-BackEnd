using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Classification
{
    public class SkillRepository : ISkillRepository
    {
        private readonly ImateDbContext _context;
        public SkillRepository(ImateDbContext context)
        {
            _context = context;
        }
        public async Task<List<int>> GetNonExistingSkillIdsAsync(IEnumerable<int> skillIds)
        {
            // 1. Xử lý danh sách rỗng/null an toàn
            if (skillIds == null || !skillIds.Any())
            {
                return new List<int>(); // Không có ID nào không tồn tại
            }

            // 2. TÌM ID TỒN TẠI (1 Query)
            var distinctIds = skillIds.Distinct().ToList();
            var existingIds = await _context.Skills
                .Where(c => distinctIds.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync();

            // 3. TÌM ID KHÔNG TỒN TẠI (LINQ Except)
            var nonExistingIds = distinctIds.Except(existingIds).ToList();

            return nonExistingIds; // Trả về danh sách [99, 101]
        }

        public async Task<List<string>> GetNonExistingSkillNames(IEnumerable<string> skillNames)
        {
            if (skillNames == null || !skillNames.Any())
            {
                return new List<string>();
            }
            var distinctNames = skillNames.Select(name => name.Trim().ToLower()).Distinct().ToList();
            var existingNames = await _context.Skills
                .Where(c => distinctNames.Contains(c.Name.ToLower()))
                .Select(c => c.Name.ToLower())
                .ToListAsync();
            var nonExistingNames = distinctNames.Except(existingNames).ToList();
            return nonExistingNames;
        }
        public async Task<List<Skill>> FindSkillsByNamesAsync(IEnumerable<string> names)
        {
            if (names == null || !names.Any())
            {
                return new List<Skill>();
            }

            // Chuyển tất cả tên sang chữ thường để tìm kiếm không phân biệt hoa/thường
            var lowerCaseNames = names.Select(n => n.ToLower()).ToList();

            return await _context.Skills
                .Where(s => lowerCaseNames.Contains(s.Name.ToLower()))
                .ToListAsync();
        }
        public IQueryable<Skill> GetAllSkills()
        {
            return _context.Skills.AsNoTracking();
        }
        public async Task<Skill> GetSkillByIdAsync(int id)
        {
            return await _context.Skills.FirstOrDefaultAsync(c => c.Id == id);
        }
        public async Task<Skill> AddSkillAsync(Skill skill)
        {
            _context.Skills.Add(skill);
            await _context.SaveChangesAsync();
            return skill;
        }
        public async Task<Skill> UpdateSkillAsync(Skill skill)
        {
            _context.Skills.Update(skill);
            await _context.SaveChangesAsync();
            return skill;
        }
        public void SaveChangeAsync()
        {
            _context.SaveChangesAsync();
        }
    }
}
