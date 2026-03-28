using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Enums;
using System.Text.Json;
using ModelsEntities = Imate.API.Models.Entities;
using AuditLogResponseModels = Imate.API.Presentation.ResponseModels.AuditLog;
using AuditLogRequestModels = Imate.API.Presentation.RequestModels.AuditLog;
using Imate.API.Business.Exceptions;

namespace Imate.API.Business.Services
{
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IAccountRepository _accountRepository;

        public AuditLogService(IAuditLogRepository auditLogRepository, IAccountRepository accountRepository)
        {
            _auditLogRepository = auditLogRepository;
            _accountRepository = accountRepository;
        }

        public async Task<AuditLogResponseModels.AuditLogDetailResponse> GetAuditLogDetailAsync(int id)
        {
            var auditLog = await _auditLogRepository.GetByIdAsync(id);

            if (auditLog == null)
            {
                throw new NotFoundException($"Audit log with ID {id} not found.");
            }

            return MapToDetailResponse(auditLog);
        }

        public async Task<AuditLogResponseModels.AuditLogFilterOptionsResponse> GetFilterOptionsAsync()
        {
            var allLogs = await _auditLogRepository.GetAllAuditLogs()
                .Select(al => new
                {
                    StaffName = al.User != null ? al.User.FullName : null,
                    Action = al.Action.ToString(),
                    EntityType = al.EntityType
                })
                .ToListAsync();

            var staffNames = allLogs
                .Where(x => !string.IsNullOrEmpty(x.StaffName))
                .Select(x => x.StaffName!)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var actions = allLogs
                .Select(x => x.Action)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            var entityTypes = allLogs
                .Select(x => x.EntityType)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            return new AuditLogResponseModels.AuditLogFilterOptionsResponse
            {
                StaffNames = staffNames,
                Actions = actions,
                EntityTypes = entityTypes
            };
        }

        public async Task<PagedList<AuditLogResponseModels.AuditLogListResponse>> GetAuditLogsAsync(AuditLogRequestModels.AuditLogParams auditLogParams)
        {
            var query = _auditLogRepository.GetAllAuditLogs();

            // Filter by Staff Name (exact match for better filtering)
            if (!string.IsNullOrWhiteSpace(auditLogParams.StaffName))
            {
                query = query.Where(al => al.User != null && al.User.FullName.ToLower() == auditLogParams.StaffName.ToLower());
            }

            // Filter by Entity Type
            if (!string.IsNullOrWhiteSpace(auditLogParams.EntityType))
            {
                query = query.Where(al => al.EntityType.ToLower() == auditLogParams.EntityType.ToLower());
            }

            // Filter by Action
            if (auditLogParams.Action.HasValue)
            {
                query = query.Where(al => al.Action == auditLogParams.Action.Value);
            }

            // Filter by SearchTerm (search in all columns: staff name, staff email, entity type, action, entity id)
            if (!string.IsNullOrWhiteSpace(auditLogParams.SearchTerm))
            {
                var searchTerm = auditLogParams.SearchTerm.ToLower().Trim();

				var isAction = Enum.TryParse<AuditAction>(searchTerm, true, out var actionEnum);

                int.TryParse(searchTerm, out var entityId);
                var isEntityId = int.TryParse(searchTerm, out entityId);

                query = query.Where(al =>
                    (al.User != null && al.User.FullName.ToLower().Contains(searchTerm)) ||
                    (al.User != null && al.User.Email != null && al.User.Email.ToLower().Contains(searchTerm)) ||
                    al.EntityType.ToLower().Contains(searchTerm) ||
                    (isAction && al.Action == actionEnum) ||
                    (isEntityId && al.EntityId == entityId)
                );
            }

            // Filter by Date Range
            if (auditLogParams.FromDate.HasValue)
            {
                query = query.Where(al => al.ActionTime >= auditLogParams.FromDate.Value);
            }

            if (auditLogParams.ToDate.HasValue)
            {
                query = query.Where(al => al.ActionTime <= auditLogParams.ToDate.Value);
            }

            // Sorting
            if (!string.IsNullOrWhiteSpace(auditLogParams.SortBy))
            {
                bool isDescending = auditLogParams.SortOrder?.ToLower() == "desc";

                query = auditLogParams.SortBy.ToLower() switch
                {
                    "actiontime" => isDescending
                        ? query.OrderByDescending(al => al.ActionTime)
                        : query.OrderBy(al => al.ActionTime),
                    "staffname" => isDescending
                        ? query.OrderByDescending(al => al.User.FullName)
                        : query.OrderBy(al => al.User.FullName),
                    "entitytype" => isDescending
                        ? query.OrderByDescending(al => al.EntityType)
                        : query.OrderBy(al => al.EntityType),
                    _ => query.OrderByDescending(al => al.ActionTime)
                };
            }
            else
            {
                query = query.OrderByDescending(al => al.ActionTime);
            }

            var pagedAuditLogs = await PagedList<ModelsEntities.AuditLog>.CreateAsync(query, auditLogParams.PageNumber, auditLogParams.PageSize);

            var responseItems = pagedAuditLogs.Items.Select(MapToListResponse).ToList();

            return new PagedList<AuditLogResponseModels.AuditLogListResponse>(
                responseItems,
                pagedAuditLogs.TotalCount,
                pagedAuditLogs.PageNumber,
                pagedAuditLogs.PageSize
            );
        }

        public async Task<ModelsEntities.AuditLog> CreateAuditLogAsync(int userId, AuditAction action, string entityType, int entityId, object? oldValue = null, object? newValue = null)
        {
            var auditLog = new ModelsEntities.AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                ActionTime = DateTime.UtcNow
            };

            // Serialize OldValue if provided
            if (oldValue != null)
            {
                auditLog.OldValue = JsonSerializer.SerializeToElement(oldValue).ToString();
            }

            // Serialize NewValue if provided
            if (newValue != null)
            {
                auditLog.NewValue = JsonSerializer.SerializeToElement(newValue).ToString();
            }

            return await _auditLogRepository.AddAsync(auditLog);
        }

        private AuditLogResponseModels.AuditLogListResponse MapToListResponse(ModelsEntities.AuditLog auditLog)
        {
            return new AuditLogResponseModels.AuditLogListResponse
            {
                Id = auditLog.Id,
                StaffName = auditLog.User?.FullName ?? "Unknown",
                StaffEmail = auditLog.User?.Email ?? "Unknown",
                Action = auditLog.Action.ToString(),
                OldValue = auditLog.OldValue,
                NewValue = auditLog.NewValue,
                EntityType = auditLog.EntityType,
                ActionTime = auditLog.ActionTime.DateTime
            };
        }

        private AuditLogResponseModels.AuditLogDetailResponse MapToDetailResponse(ModelsEntities.AuditLog auditLog)
        {
            return new AuditLogResponseModels.AuditLogDetailResponse
            {
                Id = auditLog.Id,
                StaffName = auditLog.User?.FullName ?? "Unknown",
                StaffEmail = auditLog.User?.Email ?? "Unknown",
                Action = auditLog.Action.ToString(),
                EntityType = auditLog.EntityType,
                EntityId = auditLog.EntityId,
                ActionTime = auditLog.ActionTime.DateTime,
                CreatedAt = auditLog.CreatedAt.DateTime,
                UpdatedAt = auditLog.UpdatedAt?.DateTime,
                OldValue = string.IsNullOrEmpty(auditLog.OldValue)
            ? null
            : JsonSerializer.Deserialize<object>(auditLog.OldValue),

                NewValue = string.IsNullOrEmpty(auditLog.NewValue)
            ? null
            : JsonSerializer.Deserialize<object>(auditLog.NewValue)
            };
        }
    }
}

