using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class PendingContributedParams : QueryParameters
    { // Filter theo ID (cho phép chọn nhiều)
        public int? SkillId { get; set; }
        public int? PositionId { get; set; }
        public int? CategoryId { get; set; }
        public int? CompanyId { get; set; }

        // Filter theo Enum
        public Level? Level { get; set; }

        // Sắp xếp
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
    }
}
