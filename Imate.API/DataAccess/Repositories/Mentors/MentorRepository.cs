using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Mentors
{
    public class MentorRepository : RepositoryBase<Mentor>, IMentorRepository
    {
        public MentorRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }
        public async Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync()
        {
            return await FindAll(trackChanges: false)
                .Include(m => m.Account)
                .Include(m => m.MentorPositions)
                    .ThenInclude(mp => mp.Position)
                .Include(m => m.MentorCompanies)
                    .ThenInclude(mc => mc.Company)
                .Select(m => new MentorResponse.ListPreviewMentor
                {
                    FullName = m.Account.FullName,
                    Position = m.MentorPositions.FirstOrDefault() != null ? m.MentorPositions.FirstOrDefault().Position.Name : string.Empty,
                    Yoe = m.Yoe,
                    Company = m.MentorCompanies.FirstOrDefault() != null ? m.MentorCompanies.FirstOrDefault().Company.Name : string.Empty,
                    AvgRatings = m.AvgRatings,
                    TotalRatingCount = m.TotalRatingCount
                })
                .ToListAsync();
        }
    }
}
