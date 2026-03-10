namespace Imate.API.Models.Entities
{
    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<MentorSkill> MentorSkills { get; set; } = new List<MentorSkill>();
        public ICollection<QuestionSkill> QuestionSkills { get; set; } = new List<QuestionSkill>();
        public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
    }
}
