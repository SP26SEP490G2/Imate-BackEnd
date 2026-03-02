namespace Imate.API.Models.Entities
{
    public class SavedQuestion
    {
        public int AccountId { get; set; }
        public int QuestionId { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
        public Question Question { get; set; } = null!;
    }
}
