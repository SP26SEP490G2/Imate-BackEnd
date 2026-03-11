namespace Imate.API.Presentation.ResponseModels.QuestionBank
{
    public class ContributionFormDataResponseModel
    {
        public IEnumerable<CompanyResponse> Companies { get; set; }
        public IEnumerable<CategoryResponse> Categories { get; set; }
        public IEnumerable<PositionWithSkillsResponse> Positions { get; set; }
    }

    public class CompanyResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class SkillResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class PositionWithSkillsResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
