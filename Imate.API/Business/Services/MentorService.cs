using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.Presentation.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services
{
    public class MentorService : IMentorService
    {
        private readonly ImateDbContext _context;
        public MentorService(ImateDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentors()
        {
            try
            {
                var mentors = await _context.Mentors
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
                    }).ToListAsync();
                return mentors;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy dữ liệu fake!", ex);
            }
        }
    }
}
