namespace Imate.API.Models.Entities
{
    public class Comment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Account User { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
