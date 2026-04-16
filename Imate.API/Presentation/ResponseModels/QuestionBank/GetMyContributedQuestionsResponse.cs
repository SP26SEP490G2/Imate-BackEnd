namespace Imate.API.Presentation.ResponseModels.QuestionBank
{
    public class GetMyContributedQuestionsResponse
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsActive { get; set; }
        public string? ApprovalStatus { get; set; }
        public string? SampleAnswer { get; set; }
        public int? ContributedDetailId { get; set; }
        public string Difficulty { get; set; }
        public MyContributedDetailDto? ContributedDetail { get; set; }
        public List<string> CategoriesName { get; set; }
        public List<string> SkillsName { get; set; }
        public List<string> PositionsName { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }

    public class MyContributedDetailDto
    {
        public int Id { get; set; }
        public DateOnly InterviewDate { get; set; }
        public string Level { get; set; }
        public CompanyDto? Company { get; set; }
    }

    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
    }
}

