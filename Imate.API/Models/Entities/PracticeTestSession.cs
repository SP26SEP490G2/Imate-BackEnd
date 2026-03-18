namespace Imate.API.Models.Entities
{
    /// <summary>
    /// Lưu kết quả bài test luyện tập (UC-30 → UC-31, UC-32)
    /// </summary>
    public class PracticeTestSession
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string TestTitle { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty; // "Technical" | "Language"
        public string Field { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public int Score { get; set; } // 0–100
        public int TimeLimitMinutes { get; set; }
        public int? DurationMinutes { get; set; }
        public int? TechnicalScore { get; set; }
        public int? LogicalScore { get; set; }
        public int? OptimizationScore { get; set; }
        public string? AiFeedback { get; set; }
        public string? AiStrengths { get; set; }
        public string? AiImprovements { get; set; }
        public DateTimeOffset CompletedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
        public ICollection<PracticeTestAnswer> Answers { get; set; } = new List<PracticeTestAnswer>();
    }
}
