using Imate.API.Models.Entities;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.QuestionBank
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
            public IEnumerable<QuestionBankItem> Questions { get; set; }
            public int TotalCount { get; set; }
            public int PageNumber { get; set; }
            public int PageSize { get; set; }
            public int TotalPages { get; set; }
        }

        public class QuestionCategoryItem
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
        public class GetAllSystemQuestionsForStaff
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public DifficultyLevel? Difficulty { get; set; }
            public bool IsFromSystem { get; set; }
            public bool IsActive { get; set; }
            public int CreatorId { get; set; }
            public string CreatorName { get; set; }
            public string? SampleAnswer { get; set; }
            public List<string> CategoriesName { get; set; }
            public List<string> SkillsName { get; set; }
            public List<string> PositionsName { get; set; }
        }
        public class GetAllContributedQuestionsForStaff
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public DifficultyLevel? Difficulty { get; set; }
            public bool IsFromSystem { get; set; }
            public bool IsActive { get; set; }
            public int CreatorId { get; set; }
            public string CreatorName { get; set; }
            public string? SampleAnswer { get; set; }
            public int? ContributedDetailId { get; set; } // Cho phép NULL
            public ContributedDetail ContributedDetail { get; set; } // Quan hệ 1:0..1
            public List<string> CategoriesName { get; set; }
            public List<string> SkillsName { get; set; }
            public List<string> PositionsName { get; set; }
        }
        public class GetAllSystemQuestionsForStaffAsyncResponse
        {
            public int Id { get; set; }
            public string Content { get; set; }
            public DifficultyLevel? Difficulty { get; set; }
            public bool IsFromSystem { get; set; }
            public bool IsActive { get; set; }
            public int CreatorId { get; set; }
            public string CreatorName { get; set; }
            public string? SampleAnswer { get; set; }
            public List<string> CategoriesName { get; set; }
            public List<string> SkillsName { get; set; }
            public List<string> PositionsName { get; set; }

        }
    }
}
