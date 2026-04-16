using Imate.AI.Module.Models.Responses;

namespace Imate.AI.Module.Interfaces
{
    /// <summary>
    /// Interface cho dịch vụ phỏng vấn AI
    /// UC-35: Practice Mock Interview
    /// </summary>
    public interface IInterviewService
    {
        /// <summary>Tạo tin nhắn chào mừng từ AI</summary>
        Task<string> GenerateWelcomeMessageAsync(string? cvContent, string? positionName, string? companyName, string? language = null);

        /// <summary>Tạo câu hỏi phỏng vấn tiếp theo (adaptive)</summary>
        Task<GenerateQuestionResult> GenerateQuestionAsync(int sessionId, double? estimatedAbility = null);

        /// <summary>Tạo feedback cho tất cả câu trả lời khi kết thúc phỏng vấn</summary>
        Task<string> GenerateFeedbackForSessionAsync(int sessionId);

        /// <summary>Phân loại JD bằng AI — trích xuất vị trí, kỹ năng, cấp độ</summary>
        Task<SetupInterviewResult> ClassifyJobDescriptionAsync(string jobDescriptionText);

        /// <summary>Tạo phản hồi ngắn gọn của AI sau khi ứng viên trả lời (tương tác tự nhiên)</summary>
        Task<string> GenerateReactionAsync(int sessionId, string question, string userAnswer);

        /// <summary>Phân tích khoảng cách năng lực (Gap Analysis) giữa CV và JD</summary>
        Task<string> AnalyzeGapsAsync(string cvContent, string jobDescriptionText);
    }
}
