namespace Imate.API.Models.Entities
{
    public class JobPosition
    {
        public int JobId { get; set; }
        public int PositionId { get; set; }

        // Navigation properties
        public Job Job { get; set; } = null!;
        public Position Position { get; set; } = null!;
    }
}
