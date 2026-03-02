using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Application
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public ApplicationType ApplicationType { get; set; }
        public ApplicationStatus Status { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? EvidenceUrls { get; set; } // JSON array stored as nvarchar(max)
        public string? Response { get; set; }
        public int? ReviewerId { get; set; }
        public int? BookingId { get; set; }
        public int? CommentId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Account User { get; set; } = null!;
        public Account? Reviewer { get; set; }
        public Booking? Booking { get; set; }
        public Comment? Comment { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}
