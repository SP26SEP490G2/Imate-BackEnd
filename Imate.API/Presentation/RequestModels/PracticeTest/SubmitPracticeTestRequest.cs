namespace Imate.API.Presentation.RequestModels.PracticeTest
{
    /// <summary>
    /// Request khi candidate nộp bài test
    /// </summary>
    public record SubmitPracticeTestRequest
    {
        public string TestTitle { get; init; } = string.Empty;
        public string TestType { get; init; } = string.Empty;
        public string Field { get; init; } = string.Empty;
        public string Level { get; init; } = string.Empty;
        public int TotalQuestions { get; init; }
        public int TimeLimitMinutes { get; init; }
        public int? DurationMinutes { get; init; }
        public List<SubmitAnswerItem> Answers { get; init; } = new();
    }

    public record SubmitAnswerItem
    {
        public int QuestionNumber { get; init; }
        public string QuestionText { get; init; } = string.Empty;
        public string OptionsJson { get; init; } = string.Empty;
        public string CorrectAnswer { get; init; } = string.Empty;
        public string? UserAnswer { get; init; }
        public string? Explanation { get; init; }
    }
}
