namespace Imate.API.Models.Entities
{
    public class MentorPosition
    {
        public int MentorId { get; set; }
        public int PositionId { get; set; }

        // Navigation properties
        public Mentor Mentor { get; set; } = null!;
        public Position Position { get; set; } = null!;
    }
}
