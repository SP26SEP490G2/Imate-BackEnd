using System.Text.Json.Serialization;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.RequestModels.AuditLog
{
    public class AuditLogParams
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? StaffName { get; set; }
        public string? EntityType { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public AuditAction? Action { get; set; }
        public string? SearchTerm { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SortBy { get; set; }
        public string? SortOrder { get; set; } = "desc";
    }
}

