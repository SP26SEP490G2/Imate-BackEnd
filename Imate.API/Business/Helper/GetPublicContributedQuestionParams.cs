using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class GetPublicContributedQuestionParams : QueryParameters
    {
        // Filter theo ID
        public int? SkillId { get; set; }
        public int? PositionId { get; set; }
        public int? CategoryId { get; set; }
        public int? CompanyId { get; set; }

        // Filter theo Enum
        public Level? Level { get; set; }
        public DifficultyLevel? Difficulty { get; set; }

        // Filter theo Company Name (string search)
        public string? CompanyName { get; set; }

        // Sắp xếp
        public string? SortBy { get; set; } // "content", "createdAt"
        public string? SortOrder { get; set; } = "desc"; // "asc" hoặc "desc"
    }
}

