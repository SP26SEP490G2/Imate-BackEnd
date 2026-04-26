namespace Imate.API.Models.Entities
{
    public class InterviewResponse
    {
        public int Id { get; set; }
        public int InterviewSessionId { get; set; }
        public int TurnNumber { get; set; }
        public string QuestionContent { get; set; } = string.Empty;
        public string? UserAnswer { get; set; }
        public DateTimeOffset? AnswerTimestamp { get; set; }
        public string? AIFeedback { get; set; }
        public string? SuggestedAnswer { get; set; }
        public int? ExpectedBloomLevel { get; set; }
        public int? DemonstratedBloomLevel { get; set; }
        public double? BloomScore { get; set; }
        public double? DifficultyScore { get; set; }
        public double? CognitiveLoadScore { get; set; }
        public double? IntrinsicLoad { get; set; }
        public double? ExtraneousLoad { get; set; }
        public double? TechnicalDepthScore { get; set; }
        public double? ProblemSolvingScore { get; set; }
        public double? CommunicationScore { get; set; }
        public double? PracticalExperienceScore { get; set; }
        public double? StarSituationScore { get; set; }
        public double? StarTaskScore { get; set; }
        public double? StarActionScore { get; set; }
        public double? StarResultScore { get; set; }
        public string? StructuredFeedbackJson { get; set; } // JSON stored as nvarchar(max)
        public string? ExpectedAnswerOutline { get; set; }
        public string? Topic { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public InterviewSession InterviewSession { get; set; } = null!;
    }
}
