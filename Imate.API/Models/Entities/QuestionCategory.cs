namespace Imate.API.Models.Entities
{
    public class QuestionCategory
    {
        public int QuestionId { get; set; }
        public int CategoryId { get; set; }

        // Navigation properties
        public Question Question { get; set; } = null!;
        public Category Category { get; set; } = null!;
    }
}
