using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Mentors
{
    public class SlotRepository : RepositoryBase<Slot>, ISlotRepository
    {
        public SlotRepository(ImateDbContext context) : base(context)
        {
        }

        public async Task<Slot?> GetByIdAsync(int id)
        {
            return await FindByCondition(s => s.Id == id, false).FirstOrDefaultAsync();
        }
    }
}
