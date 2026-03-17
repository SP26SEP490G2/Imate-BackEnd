using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Mentors
{
    public class MentorRecurringSlotRepository : RepositoryBase<MentorRecurringSlot>, IMentorRecurringSlotRepository
    {
        public MentorRecurringSlotRepository(ImateDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<MentorRecurringSlot>> GetByMentorIdAsync(int mentorId)
        {
            return await FindByCondition(mrs => mrs.MentorId == mentorId && mrs.IsActive, false)
                .Include(mrs => mrs.Slot)
                .ToListAsync();
        }
    }
}
