using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces
{
    public interface ISystemConfigRepository
    {
        Task<SystemConfig?> GetByKeyAsync(string key);
        Task<IEnumerable<SystemConfig>> GetAllAsync();
        Task<SystemConfig?> GetByIdAsync(int id);
        Task AddAsync(SystemConfig config);
        Task UpdateAsync(SystemConfig config);
        Task<bool> ExistsAsync(string key);
    }
}

