namespace Imate.API.Presentation.ResponseModels.AuditLog
{
    public class AuditLogFilterOptionsResponse
    {
        public List<string> StaffNames { get; set; } = new List<string>();
        public List<string> Actions { get; set; } = new List<string>();
        public List<string> EntityTypes { get; set; } = new List<string>();
    }
}

