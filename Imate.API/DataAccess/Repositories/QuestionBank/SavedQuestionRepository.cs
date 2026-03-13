using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.QuestionBank
{
    public class SavedQuestionRepository : ISavedQuestionRepository
    {
        private readonly ImateDbContext _context;

        public SavedQuestionRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<SavedQuestion?> GetByAccountAndQuestionAsync(int accountId, int questionId)
        {
            return await _context.SavedQuestions
                .FirstOrDefaultAsync(sq => sq.AccountId == accountId && sq.QuestionId == questionId);
        }

        public async Task<SavedQuestion> AddAsync(SavedQuestion savedQuestion)
        {
            await _context.SavedQuestions.AddAsync(savedQuestion);
            await _context.SaveChangesAsync();
            return savedQuestion;
        }

        public async Task DeleteAsync(SavedQuestion savedQuestion)
        {
            _context.SavedQuestions.Remove(savedQuestion);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsQuestionSavedAsync(int accountId, int questionId)
        {
            return await _context.SavedQuestions
                .AnyAsync(sq => sq.AccountId == accountId && sq.QuestionId == questionId);
        }
        public async Task<IEnumerable<Question>> GetSavedSystemQuestionsByAccountAsync(int accountId)
        {
            var savedQuestionIds = await _context.SavedQuestions
                .Where(sq => sq.AccountId == accountId)
                .Select(sq => sq.QuestionId)
                .ToListAsync();

            return await _context.Questions
                .Where(q => savedQuestionIds.Contains(q.Id) && q.IsFromSystem && q.IsActive)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetSavedContributedQuestionsByAccountAsync(int accountId)
        {
            var savedQuestionIds = await _context.SavedQuestions
                .Where(sq => sq.AccountId == accountId)
                .Select(sq => sq.QuestionId)
                .ToListAsync();

            return await _context.Questions
                .Where(q => savedQuestionIds.Contains(q.Id) && q.IsFromSystem == false && q.IsActive)
                .Include(q => q.Creator)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .Include(q => q.ContributedDetail)
                    .ThenInclude(cd => cd.Company)
                .Include(q => q.Comments)
                    .ThenInclude(c => c.User)
                .Include(q => q.Comments)
                    .ThenInclude(c => c.Votes)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }
    }
}
