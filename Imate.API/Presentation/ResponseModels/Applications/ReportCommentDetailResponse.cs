namespace Imate.API.Presentation.ResponseModels.Applications
{
    public class ReportCommentDetailResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ApplicationType { get; set; } = string.Empty;
        public string? EvidenceUrls { get; set; }
        public string? Response { get; set; }
        public DateOnly CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ReportCommentUserInfo Reporter { get; set; } = new();
        public int? ReviewerId { get; set; }
        public string? ReviewerName { get; set; }
        public int? CommentId { get; set; }
        public ReportCommentDetail? CommentDetail { get; set; }
    }

    public class ReportCommentUserInfo
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? AvatarUrl { get; set; }
    }

    public class ReportCommentDetail
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Người viết comment
        public ReportCommentUserInfo Author { get; set; } = new();

        // Câu hỏi chứa comment
        public ReportCommentQuestionInfo? Question { get; set; }
    }

    public class ReportCommentQuestionInfo
    {
        public int Id { get; set; }
        public string? Content { get; set; }
        public ReportCommentUserInfo? CreatedByUser { get; set; }
    }
}
