namespace Imate.API.Models.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<QuestionCategory> QuestionCategories { get; set; } = new List<QuestionCategory>();
    }
}
