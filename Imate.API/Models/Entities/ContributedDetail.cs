using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class ContributedDetail
    {
        public int Id { get; set; }
        public DateOnly InterviewDate { get; set; }
        public Level Level { get; set; }
        public int CompanyId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Company Company { get; set; } = null!;
        public ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
