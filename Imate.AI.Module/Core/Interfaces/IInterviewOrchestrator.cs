using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Orchestrator phỏng vấn AI (Tầng 2 - Orchestrators)
    /// Điều phối workflow: data access + Agents + TTS + background tasks
    /// </summary>
    public interface IInterviewOrchestrator
    {
        /// <summary>Phân loại JD + validate CV bằng AI</summary>
        Task<SetupInterviewResult> SetupInterviewAsync(int accountId, string jobDescriptionText, int? cvId = null);

        /// <summary>Tạo phiên phỏng vấn mới</summary>
        Task<int> CreateSessionAsync(int accountId, CreateInterviewSessionRequest request);

        /// <summary>Lấy lời chào AI + TTS</summary>
        Task<WelcomeMessageResult> GetWelcomeMessageAsync(int accountId, int sessionId, CancellationToken cancellationToken);

        /// <summary>Tạo câu hỏi tiếp theo + TTS</summary>
        Task<GenerateQuestionResult> GenerateQuestionAsync(int accountId, int sessionId, double? estimatedAbility, CancellationToken cancellationToken);

        /// <summary>Lưu câu trả lời + tạo AI reaction + TTS</summary>
        Task<SubmitAnswerResult> SubmitAnswerAsync(int accountId, SubmitAnswerRequest request, CancellationToken cancellationToken);

        /// <summary>Kết thúc phỏng vấn — chạy nền tạo feedback</summary>
        Task EndInterviewAsync(int accountId, int sessionId);

        /// <summary>Xem kết quả phỏng vấn chi tiết</summary>
        Task<InterviewResultData> GetInterviewResultAsync(int accountId, int sessionId);

        /// <summary>Khôi phục trạng thái phiên khi reload</summary>
        Task<ResumeSessionData> ResumeSessionAsync(int accountId, int sessionId);

        /// <summary>Danh sách lịch sử phỏng vấn</summary>
        Task<List<InterviewHistoryItem>> GetInterviewHistoryAsync(int accountId);

        /// <summary>Kiểm tra chi phí phỏng vấn</summary>
        Task<InterviewLimitStatus> CheckInterviewCostAsync(int accountId);
    }

    // ── DTOs cho Orchestrator responses ──

    public class WelcomeMessageResult
    {
        public string WelcomeMessage { get; set; } = string.Empty;
        public string? AudioBase64 { get; set; }
        public string? MimeType { get; set; }
    }

    public class SubmitAnswerResult
    {
        public string? AiReaction { get; set; }
        public string? AiReactionAudioBase64 { get; set; }
        public string? MimeType { get; set; }
    }

    public class InterviewResultData
    {
        public object Session { get; set; } = null!;
        public object Responses { get; set; } = null!;
    }

    public class ResumeSessionData
    {
        public object Session { get; set; } = null!;
        public object Responses { get; set; } = null!;
        public int AnsweredCount { get; set; }
        public int? CurrentResponseId { get; set; }
        public bool HasUnansweredQuestion { get; set; }
    }

    public class InterviewHistoryItem
    {
        public int Id { get; set; }
        public string? PositionName { get; set; }
        public string? SkillName { get; set; }
        public string? LevelName { get; set; }
        public string? CompanyName { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset? EndTime { get; set; }
        public int TotalQuestionsAnswered { get; set; }
        public double? EstimatedAbility { get; set; }
        public string? Status { get; set; }
        public string? InterviewType { get; set; }
    }
}