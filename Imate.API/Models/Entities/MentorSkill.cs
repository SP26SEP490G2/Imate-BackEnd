namespace Imate.API.Models.Entities
{
    public class MentorSkill
    {
        public int MentorId { get; set; }
        public int SkillId { get; set; }

        // Navigation properties
        public Mentor Mentor { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
