namespace Imate.AI.Module.Models.Responses
{
    /// <summary>
    /// Kết quả tạo câu hỏi phỏng vấn
    /// </summary>
    public class GenerateQuestionResult
    {
        public int InterviewResponseId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string? ExpectedAnswerOutline { get; set; }
        public string? Topic { get; set; }
        public bool IsTerminated { get; set; }
        public string? TerminationReason { get; set; }
        public string? TerminationMessage { get; set; }
        public QuestionMetrics? Metrics { get; set; }
        public string? AudioBase64 { get; set; }
        public string? MimeType { get; set; }
    }

    /// <summary>
    /// Metrics đi kèm câu hỏi
    /// </summary>
    public class QuestionMetrics
    {
        public BloomInfo? BloomTaxonomy { get; set; }
        public IrtInfo? Irt { get; set; }
        public CltInfo? Clt { get; set; }
        public string? QuestionType { get; set; }
    }

    public class BloomInfo
    {
        public int Level { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class IrtInfo
    {
        public double DifficultyScore { get; set; }
        public double EstimatedAbility { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }

    public class CltInfo
    {
        public double TotalCognitiveLoad { get; set; }
        public string Interpretation { get; set; } = string.Empty;
    }

    /// <summary>
    /// Kết quả đánh giá câu trả lời từ AI
    /// </summary>
    public class FeedbackResult
    {
        public string OverallComment { get; set; } = string.Empty;
        public List<string> Strengths { get; set; } = new();
        public List<string> Improvements { get; set; } = new();
        public string? SuggestedAnswer { get; set; }

        // Scores
        public double? BloomScore { get; set; }
        public int? DemonstratedBloomLevel { get; set; }
        public double? TechnicalDepthScore { get; set; }
        public double? ProblemSolvingScore { get; set; }
        public double? CommunicationScore { get; set; }
        public double? PracticalExperienceScore { get; set; }
    }

    /// <summary>
    /// Kết quả phân loại JD — UC-34: Setup Interview
    /// </summary>
    public class SetupInterviewResult
    {
        public string Position { get; set; } = string.Empty;
        public string Skill { get; set; } = string.Empty;
        public string[] Skills { get; set; } = Array.Empty<string>();
        public string Level { get; set; } = string.Empty;
        public string? Company { get; set; }
        public string[]? Requirements { get; set; }
        public string? LevelMismatchWarning { get; set; }
    }

    /// <summary>
    /// Thông tin chi phí phỏng vấn
    /// </summary>
    public class InterviewCostResult
    {
        public bool RequiresPayment { get; set; }
        public bool IsFree { get; set; }
        public int FreeUsedMock { get; set; }
        public int FreeLimit { get; set; }
        public int RemainingFree { get; set; }
        public bool HasEnoughBalance { get; set; }
    }
}
