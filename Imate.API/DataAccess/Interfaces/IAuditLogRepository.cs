using Imate.API.Models.Entities;
using ModelsEntities = Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<ModelsEntities.AuditLog?> GetByIdAsync(int id);
        Task<ModelsEntities.AuditLog> AddAsync(ModelsEntities.AuditLog auditLog);
        IQueryable<ModelsEntities.AuditLog> GetAllAuditLogs();
    }
}

