using System.Text.Json;

namespace Imate.API.Presentation.ResponseModels.AuditLog
{
    public class AuditLogDetailResponse
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public string StaffEmail { get; set; }
        public string Action { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public DateTime ActionTime { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public object? OldValue { get; set; }
        public object? NewValue { get; set; }
    }
}

