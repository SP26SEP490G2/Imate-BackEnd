using Imate.API.Entities;
using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class InterviewSession
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? UserCvId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public InterviewStatus Status { get; set; }
        public string? OverallFeedback { get; set; }
        public InterviewType InterviewType { get; set; }
        public int? QuestionId { get; set; }
        public string? PositionName { get; set; }
        public string? SkillName { get; set; }
        public string? LevelName { get; set; }
        public string? CompanyName { get; set; }
        public string? JobDescriptionText { get; set; }
        public double? EstimatedAbility { get; set; }
        public int TotalQuestionsAnswered { get; set; } = 0;
        public string? CvContent { get; set; }
        public string? ExtractedSkillsJson { get; set; } // JSON stored as nvarchar(max)
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public int? TrainingJourneyId { get; set; }
        public string? SessionGapJson { get; set; }  // Các gap được chọn cho session này

        // Navigation properties
        public Account Account { get; set; } = null!;
        public UserCv? UserCv { get; set; }
        public Question? Question { get; set; }
        public ICollection<InterviewResponse> InterviewResponses { get; set; } = new List<InterviewResponse>();
        public virtual TrainingJourney? TrainingJourney { get; set; }
    }
}
