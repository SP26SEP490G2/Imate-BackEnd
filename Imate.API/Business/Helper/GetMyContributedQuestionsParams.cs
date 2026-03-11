using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class GetMyContributedQuestionsParams : QueryParameters
    {
        // Filter theo ApprovalStatus
        public QuestionApprovalStatus? ApprovalStatus { get; set; }

        // Filter theo Skill, Position, Category (optional)
        public int? SkillId { get; set; }
        public int? PositionId { get; set; }
        public int? CategoryId { get; set; }

        // Filter theo Level
        public Level? Level { get; set; }

        // Sắp xếp
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "desc"; // "asc" hoặc "desc"
    }
}

