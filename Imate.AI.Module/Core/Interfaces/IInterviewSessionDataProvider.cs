
namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Interface truy cập dữ liệu phỏng vấn.
    /// AI Module dùng DTO riêng, host project (Imate.API) implement và map từ entity.
    /// Giống pattern ICvDataProvider / IQuestionDataProvider.
    /// </summary>
    public interface IInterviewSessionDataProvider
    {
        // ── Session ──
        Task<int> CreateSessionAsync(InterviewSessionData session);
        Task<InterviewSessionData?> GetSessionByIdAsync(int id);
        Task UpdateSessionAsync(InterviewSessionData session);
        Task<List<InterviewSessionData>> GetSessionsByAccountIdAsync(int accountId);

        // ── Response ──
        Task<int> CreateResponseAsync(InterviewResponseData response);
        Task<InterviewResponseData?> GetResponseByIdAsync(int id);
        Task UpdateResponseAsync(InterviewResponseData response);
        Task<List<InterviewResponseData>> GetResponsesBySessionIdAsync(int sessionId);
        
        // ── Limits & Usage ──
        Task<InterviewLimitStatus> GetInterviewLimitStatusAsync(int accountId);
        Task IncrementMockInterviewUsageAsync(int accountId);
    }

    /// <summary>
    /// DTO đại diện cho InterviewSession (không phụ thuộc Imate.API entities)
    /// </summary>
    public class InterviewSessionData
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? UserCvId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public string Status { get; set; } = "InProgress"; // InProgress, Completed, Cancelled
        public string? OverallFeedback { get; set; }
        public string InterviewType { get; set; } = "FullSession";
        public int? QuestionId { get; set; }
        public string? PositionName { get; set; }
        public string? SkillName { get; set; }
        public string? LevelName { get; set; }
        public string? CompanyName { get; set; }
        public string? JobDescriptionText { get; set; }
        public double? EstimatedAbility { get; set; }
        public int TotalQuestionsAnswered { get; set; }
        public string? CvContent { get; set; }
        public string? ExtractedSkillsJson { get; set; }
        public int? TrainingJourneyId { get; set; }
        public string? SessionGapJson { get; set; }
        public string? GapAnalysisJson { get; set; }
    }

    /// <summary>
    /// DTO đại diện cho InterviewResponse (không phụ thuộc Imate.API entities)
    /// </summary>
    public class InterviewResponseData
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
        public string? StructuredFeedbackJson { get; set; }
        public string? ExpectedAnswerOutline { get; set; }
        public string? Topic { get; set; }
    }

    /// <summary>
    /// Thông tin giới hạn lượt phỏng vấn của người dùng.
    /// </summary>
    public class InterviewLimitStatus
    {
        public bool CanStart { get; set; }
        public int UsedCount { get; set; }
        public int LimitCount { get; set; }
        public int RemainingCount { get; set; }
        public bool IsFree { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}