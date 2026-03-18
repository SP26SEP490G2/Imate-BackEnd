namespace Imate.API.Models.Entities
{
    /// <summary>
    /// Lưu chi tiết từng câu trả lời trong bài test luyện tập
    /// </summary>
    public class PracticeTestAnswer
    {
        public int Id { get; set; }
        public int PracticeTestSessionId { get; set; }
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string OptionsJson { get; set; } = string.Empty; // JSON array of options
        public string CorrectAnswer { get; set; } = string.Empty; // "A" | "B" | "C" | "D"
        public string? UserAnswer { get; set; }
        public bool IsCorrect { get; set; }
        public string? Explanation { get; set; }

        // Navigation properties
        public PracticeTestSession PracticeTestSession { get; set; } = null!;
    }
}
