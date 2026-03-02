namespace Imate.API.Models.Entities
{
    public class QuestionSkill
    {
        public int QuestionId { get; set; }
        public int SkillId { get; set; }

        // Navigation properties
        public Question Question { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
