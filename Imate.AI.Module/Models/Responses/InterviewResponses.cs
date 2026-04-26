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

        /// <summary>Giai đoạn phỏng vấn (1-5)</summary>
        public int ChunkIndex { get; set; }
        /// <summary>Tên giai đoạn hiển thị cho ứng viên</summary>
        public string ChunkLabel { get; set; } = string.Empty;
        public bool IsValid { get; set; } = true;
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

        /// <summary>CV có thuộc ngành IT không? (false nếu ngành khác)</summary>
        public bool IsItRelatedCv { get; set; } = true;

        /// <summary>JD có thuộc ngành IT không? (false nếu ngành khác)</summary>
        public bool IsItRelatedJd { get; set; } = true;

        /// <summary>Level ước tính từ CV (Intern/Fresher/Junior/Middle/Senior/Lead)</summary>
        public string? CvEstimatedLevel { get; set; }
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
