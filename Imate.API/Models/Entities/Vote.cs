namespace Imate.API.Models.Entities
{
    public class Vote
    {
        public int AccountId { get; set; }
        public int CommentId { get; set; }
        public bool IsUpvote { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
        public Comment Comment { get; set; } = null!;
    }
}
