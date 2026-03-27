namespace Imate.API.Presentation.ResponseModels.AuditLog
{
    public class AuditLogListResponse
    {
        public int Id { get; set; }
        public string StaffName { get; set; }
        public string StaffEmail { get; set; }
        public string Action { get; set; }
		public string? OldValue { get; set; }
		public string? NewValue { get; set; }
		public string EntityType { get; set; }
        public DateTime ActionTime { get; set; }
    }
}

