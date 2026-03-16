using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Repositories
{
    public class PracticeTestSessionRepository : IPracticeTestSessionRepository
    {
        private readonly ImateDbContext _context;

        public PracticeTestSessionRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PracticeTestSession>> GetByAccountIdAsync(int accountId)
        {
            return await _context.PracticeTestSessions
                .AsNoTracking()
                .Where(s => s.AccountId == accountId)
                .OrderByDescending(s => s.CompletedAt)
                .ToListAsync();
        }

        public async Task<PracticeTestSession?> GetByIdWithAnswersAsync(int id)
        {
            return await _context.PracticeTestSessions
                .AsNoTracking()
                .Include(s => s.Answers.OrderBy(a => a.QuestionNumber))
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(PracticeTestSession session)
        {
            _context.PracticeTestSessions.Add(session);
            await _context.SaveChangesAsync();
        }
    }
}
