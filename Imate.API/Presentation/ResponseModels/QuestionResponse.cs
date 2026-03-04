namespace Imate.API.Presentation.ResponseModels
{
    public class QuestionResponse
    {
        public class ListHotQuestion
        {
            public int Id { get; set; }
            public string Content { get; set; } = string.Empty;
            public List<string> Categories { get; set; } = new List<string>();
            public int CommentCount { get; set; }
        }

        public class QuestionBankItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public List<string> Categories { get; set; } = new List<string>();
            public List<string> Skills { get; set; } = new List<string>();
            public string? Difficulty { get; set; }
            public int CommentCount { get; set; }
            public string CreatedBy { get; set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; set; }
        }

        public class QuestionBankList
        {
            public IEnumerable<QuestionBankItem> Questions { get; set; } = new List<QuestionBankItem>();
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
        }

        public class CategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}
