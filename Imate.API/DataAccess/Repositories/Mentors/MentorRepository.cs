using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Mentors
{
    public class MentorRepository : RepositoryBase<Mentor>, IMentorRepository
    {
        private readonly ImateDbContext _context;
        public MentorRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
            _context = repositoryContext;
        }
        public async Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync()
        {
            return await FindAll(trackChanges: false)
                .Include(m => m.Account)
                .Include(m => m.MentorPositions)
                    .ThenInclude(mp => mp.Position)
                .Include(m => m.MentorCompanies)
                    .ThenInclude(mc => mc.Company)
                .Where(m => m.Account.Status == AccountStatus.Active)
                .Select(m => new MentorResponse.ListPreviewMentor
                {
                    AccountId = m.AccountId,
                    FullName = m.Account.FullName,
                    Position = m.MentorPositions.FirstOrDefault() != null ? m.MentorPositions.FirstOrDefault().Position.Name : string.Empty,
                    Yoe = m.Yoe,
                    Company = m.MentorCompanies.FirstOrDefault() != null ? m.MentorCompanies.FirstOrDefault().Company.Name : string.Empty,
                    AvgRatings = m.AvgRatings,
                    TotalRatingCount = m.TotalRatingCount
                })
                .ToListAsync();
        }

        public async Task<Mentor> GetMentorByIdAsync(int id)
        {
            var mentor = await _context.Mentors.
                Include(m => m.Account)
                .Include(m => m.MentorSkills).ThenInclude(ms => ms.Skill)
                .Include(m => m.MentorPositions).ThenInclude(mp => mp.Position)
                .Include(m => m.MentorCompanies).ThenInclude(mc => mc.Company)
                .Where(Mentors => Mentors.AccountId == id).
                FirstOrDefaultAsync(m => m.AccountId == id);
            return mentor;
        }
        public async Task<Mentor?> GetByIdAsync(int id)
        {
            return await FindByCondition(m => m.AccountId == id, false).FirstOrDefaultAsync();
        }

        public async Task<Mentor?> GetByIdWithSkillsAsync(int id)
        {
            return await FindByCondition(m => m.AccountId == id, false)
                .Include(m => m.MentorSkills)
                    .ThenInclude(ms => ms.Skill)
                .FirstOrDefaultAsync();
        }

        public async Task<Mentor> UpdateMentorAsync(Mentor mentor)
        {
            _context.Mentors.Update(mentor);
            await _context.SaveChangesAsync();
            return mentor;
        }
    }
}
