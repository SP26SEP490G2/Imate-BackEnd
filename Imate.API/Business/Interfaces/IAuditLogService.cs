using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.AuditLog;
using Imate.API.Presentation.ResponseModels.AuditLog;

namespace Imate.API.Business.Interfaces
{
    public interface IAuditLogService
    {
        Task<AuditLogDetailResponse> GetAuditLogDetailAsync(int id);
        Task<PagedList<AuditLogListResponse>> GetAuditLogsAsync(AuditLogParams auditLogParams);
        Task<AuditLog> CreateAuditLogAsync(int userId, AuditAction action, string entityType, int entityId, object? oldValue = null, object? newValue = null);
        Task<AuditLogFilterOptionsResponse> GetFilterOptionsAsync();
    }
}

