namespace Imate.API.Presentation.ResponseModels.Classification
{
    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int QuestionCount { get; set; }
    }
}
