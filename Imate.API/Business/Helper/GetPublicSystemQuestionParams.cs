using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class GetPublicSystemQuestionParams : QueryParameters
    {
        // Filter theo ID
        public int? SkillId { get; set; }
        public int? PositionId { get; set; }
        public int? CategoryId { get; set; }

        // Filter theo Enum
        public DifficultyLevel? Difficulty { get; set; }

        // Sắp xếp
        public string? SortBy { get; set; } // "content", "createdAt"
        public string? SortOrder { get; set; } = "desc"; // "asc" hoặc "desc"
    }
}

