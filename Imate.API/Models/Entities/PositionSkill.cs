namespace Imate.API.Models.Entities
{
    public class PositionSkill
    {
        public int PositionId { get; set; }
        public int SkillId { get; set; }

        // Navigation properties
        public Position Position { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }
}
