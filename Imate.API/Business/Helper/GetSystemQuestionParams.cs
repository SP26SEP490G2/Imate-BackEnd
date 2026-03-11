using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class GetSystemQuestionParams:QueryParameters
    {
        // Filter theo ID (cho phép chọn nhiều)
        public int? SkillId { get; set; }
        public int? PositionId { get; set; }
        public int? CategoryId { get; set; }

        // Filter theo Enum
        public DifficultyLevel? Difficulty { get; set; } 
     
        // Filter theo trạng thái
        public bool? IsActive { get; set; }

        // Sắp xếp
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
    }
}
