using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Mentors
{
    public interface IMentorRecurringSlotRepository : IRepositoryBase<MentorRecurringSlot>
    {
        Task<IEnumerable<MentorRecurringSlot>> GetByMentorIdAsync(int mentorId);
    }
}
