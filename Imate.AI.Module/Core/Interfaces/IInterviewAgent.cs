using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Agent phỏng vấn AI (Tầng 3 - Agents)
    /// Chịu trách nhiệm: build prompt, gọi AI Service, parse response
    /// </summary>
    public interface IInterviewAgent
    {
        /// <summary>Tạo tin nhắn chào mừng từ AI</summary>
        Task<string> GenerateWelcomeMessageAsync(string? cvContent, string? positionName, string? companyName, string? language = null);

        /// <summary>Tạo câu hỏi phỏng vấn tiếp theo (adaptive) — build prompt + parse JSON</summary>
        Task<GenerateQuestionResult> GenerateQuestionAsync(
                    InterviewSessionData session,
                    List<InterviewResponseData> existingResponses,
                    double? estimatedAbility = null,
                    List<string>? selectedGaps = null);
        /// <summary>Phân loại JD bằng AI — trích xuất vị trí, kỹ năng, cấp độ</summary>
        Task<SetupInterviewResult> ClassifyJobDescriptionAsync(string jobDescriptionText, string? cvText = null);

        /// <summary>Tạo phản hồi ngắn gọn của AI sau khi ứng viên trả lời (mentor reaction)</summary>
        Task<string> GenerateReactionAsync(string? gapAnalysisJson, string question, string userAnswer);

        /// <summary>Phân tích khoảng cách năng lực (Gap Analysis) giữa CV và JD</summary>
        Task<string> AnalyzeGapsAsync(string cvContent, string jobDescriptionText);
    }
}