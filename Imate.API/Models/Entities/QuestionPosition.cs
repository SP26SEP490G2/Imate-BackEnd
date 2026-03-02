namespace Imate.API.Models.Entities
{
    public class QuestionPosition
    {
        public int QuestionId { get; set; }
        public int PositionId { get; set; }

        // Navigation properties
        public Question Question { get; set; } = null!;
        public Position Position { get; set; } = null!;
    }
}
