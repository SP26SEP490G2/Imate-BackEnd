using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Mentors
{
    public interface ISlotRepository : IRepositoryBase<Slot>
    {
        Task<Slot?> GetByIdAsync(int id);
    }
}
