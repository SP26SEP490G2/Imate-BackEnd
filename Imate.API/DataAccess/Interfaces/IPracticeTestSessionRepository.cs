using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IPracticeTestSessionRepository
    {
        Task<IEnumerable<PracticeTestSession>> GetByAccountIdAsync(int accountId);
        Task<PracticeTestSession?> GetByIdWithAnswersAsync(int id);
        Task AddAsync(PracticeTestSession session);
    }
}
