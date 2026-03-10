namespace Imate.API.Models.Entities
{
    public class Position
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<MentorPosition> MentorPositions { get; set; } = new List<MentorPosition>();
        public ICollection<QuestionPosition> QuestionPositions { get; set; } = new List<QuestionPosition>();
        public ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();
    }
}
