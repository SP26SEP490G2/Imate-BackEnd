namespace Imate.API.Presentation.ResponseModels.PracticeTest
{
    /// <summary>
    /// UC-31: Item trong danh sách lịch sử test
    /// </summary>
    public record TestHistoryItemResponse
    {
        public int Id { get; init; }
        public string TestTitle { get; init; } = string.Empty;
        public string TestType { get; init; } = string.Empty;
        public string Field { get; init; } = string.Empty;
        public string Level { get; init; } = string.Empty;
        public int Score { get; init; }
        public int TotalQuestions { get; init; }
        public int CorrectAnswers { get; init; }
        public DateTimeOffset CompletedAt { get; init; }
    }

    /// <summary>
    /// UC-32: Chi tiết kết quả test
    /// </summary>
    public record TestHistoryDetailResponse
    {
        public int Id { get; init; }
        public string TestTitle { get; init; } = string.Empty;
        public string TestType { get; init; } = string.Empty;
        public string Field { get; init; } = string.Empty;
        public string Level { get; init; } = string.Empty;
        public int TotalQuestions { get; init; }
        public int CorrectAnswers { get; init; }
        public int Score { get; init; }
        public int TimeLimitMinutes { get; init; }
        public int? DurationMinutes { get; init; }
        public int? TechnicalScore { get; init; }
        public int? LogicalScore { get; init; }
        public int? OptimizationScore { get; init; }
        public string? AiFeedback { get; init; }
        public string? AiStrengths { get; init; }
        public string? AiImprovements { get; init; }
        public DateTimeOffset CompletedAt { get; init; }
        public List<TestAnswerDetailResponse> Answers { get; init; } = new();
    }

    public record TestAnswerDetailResponse
    {
        public int QuestionNumber { get; init; }
        public string QuestionText { get; init; } = string.Empty;
        public string OptionsJson { get; init; } = string.Empty;
        public string CorrectAnswer { get; init; } = string.Empty;
        public string? UserAnswer { get; init; }
        public bool IsCorrect { get; init; }
        public string? Explanation { get; init; }
    }
}
