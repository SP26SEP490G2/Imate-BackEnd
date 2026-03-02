namespace Imate.API.Models.Entities
{
    public class SystemNotification
    {
        public int Id { get; set; }
        public int RecipientUserId { get; set; }
        public int? TriggerByUserId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Link { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation properties
        public Account RecipientUser { get; set; } = null!;
        public Account? TriggerByUser { get; set; }
    }
}
