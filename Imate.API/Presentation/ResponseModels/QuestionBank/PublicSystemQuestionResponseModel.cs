namespace Imate.API.Presentation.ResponseModels.QuestionBank
{
    public class PublicSystemQuestionResponseModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Difficulty { get; set; }
        public string SampleAnswer { get; set; }
        public string CreatorName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<CategoryDto> Categories { get; set; }
        public List<SkillDto> Skills { get; set; }
        public List<PositionDto> Positions { get; set; }
        public bool IsSaved { get; set; }
        public int CommentCount { get; set; }
    }
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SkillDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class PositionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
