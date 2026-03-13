using Microsoft.EntityFrameworkCore;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;

namespace Imate.API.DataAccess.Repositories
{
    public class SystemConfigRepository : ISystemConfigRepository
    {
        private readonly ImateDbContext _context;

        public SystemConfigRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<SystemConfig?> GetByKeyAsync(string key)
        {
            return await _context.SystemConfigs
                .FirstOrDefaultAsync(sc => sc.Key == key);
        }

        public async Task<IEnumerable<SystemConfig>> GetAllAsync()
        {
            return await _context.SystemConfigs
                .OrderBy(sc => sc.Key)
                .ToListAsync();
        }

        public async Task<SystemConfig?> GetByIdAsync(int id)
        {
            return await _context.SystemConfigs.FindAsync(id);
        }

        public async Task AddAsync(SystemConfig config)
        {
            await _context.SystemConfigs.AddAsync(config);
        }

        public Task UpdateAsync(SystemConfig config)
        {
            _context.SystemConfigs.Update(config);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _context.SystemConfigs
                .AnyAsync(sc => sc.Key == key);
        }
    }
}

