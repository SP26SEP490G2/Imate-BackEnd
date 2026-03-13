using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.DataAccess.Interfaces.QuestionBank
{
    public interface ISavedQuestionRepository
    {
        Task<SavedQuestion?> GetByAccountAndQuestionAsync(int accountId, int questionId);
        Task<SavedQuestion> AddAsync(SavedQuestion savedQuestion);
        Task DeleteAsync(SavedQuestion savedQuestion);
        Task<bool> IsQuestionSavedAsync(int accountId, int questionId);
        Task<IEnumerable<Question>> GetSavedSystemQuestionsByAccountAsync(int accountId);
        Task<IEnumerable<Question>> GetSavedContributedQuestionsByAccountAsync(int accountId);
    }
}
