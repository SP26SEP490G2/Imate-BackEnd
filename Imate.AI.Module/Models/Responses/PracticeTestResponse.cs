namespace Imate.AI.Module.Models.Responses
{
    /// <summary>
    /// Response cho bài test luyện tập sinh bởi AI
    /// UC-30: Practice Test
    /// </summary>
    public class PracticeTestResponse
    {
        public string TestTitle { get; set; } = string.Empty;
        public string TestType { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public int TimeLimitMinutes { get; set; }
        public List<PracticeTestQuestion> Questions { get; set; } = new();
    }

    public class PracticeTestQuestion
    {
        public int Id { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public List<PracticeTestOption> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
    }

    public class PracticeTestOption
    {
        public string Label { get; set; } = string.Empty; // "A", "B", "C", "D"
        public string Text { get; set; } = string.Empty;
    }
}
