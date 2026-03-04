using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using ModelsEntities = Imate.API.Models.Entities;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories
{
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly ImateDbContext _context;

        public AuditLogRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<ModelsEntities.AuditLog?> GetByIdAsync(int id)
        {
            return await _context.AuditLogs
                .Include(al => al.User)
                .FirstOrDefaultAsync(al => al.Id == id);
        }

        public async Task<ModelsEntities.AuditLog> AddAsync(ModelsEntities.AuditLog auditLog)
        {
            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
            return auditLog;
        }

        public IQueryable<ModelsEntities.AuditLog> GetAllAuditLogs()
        {
            return _context.AuditLogs
                .Include(al => al.User)
                .AsQueryable();
        }
    }
}

