using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DifficultyLevel? Difficulty { get; set; }
        public bool IsFromSystem { get; set; }
        public bool IsActive { get; set; }
        public int CreatorId { get; set; }
        public string? SampleAnswer { get; set; }
        public int? ContributedDetailId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public QuestionApprovalStatus? ApprovalStatus { get; set; } // Trạng thái duyệt: Pending, Approved, Rejected (null cho system questions)

        // Navigation properties
        public Account Creator { get; set; } = null!;
        public ContributedDetail? ContributedDetail { get; set; }
        public ICollection<SavedQuestion> SavedQuestions { get; set; } = new List<SavedQuestion>();
        public ICollection<QuestionCategory> QuestionCategories { get; set; } = new List<QuestionCategory>();
        public ICollection<QuestionSkill> QuestionSkills { get; set; } = new List<QuestionSkill>();
        public ICollection<QuestionPosition> QuestionPositions { get; set; } = new List<QuestionPosition>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
    }
}
