namespace Imate.API.Presentation.RequestModels
{
    public class QuestionRequest
    {
        public class GetQuestionBankList
        {
            public string? SearchTerm { get; set; }
            public int? CategoryId { get; set; }
            public string? Difficulty { get; set; }
            public string? SortBy { get; set; } = "newest";
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 5;
        }
    }
}
