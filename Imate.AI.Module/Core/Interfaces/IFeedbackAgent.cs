using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Agent tạo feedback phỏng vấn (Tầng 3 - Agents)
    /// Chịu trách nhiệm: build prompt feedback, parse response AI
    /// </summary>
    public interface IFeedbackAgent
    {
        /// <summary>Tạo feedback cho một câu trả lời cụ thể</summary>
        Task<FeedbackResult> GeneratePerQuestionFeedbackAsync(InterviewResponseData response, string? gapAnalysis = null);

        /// <summary>Tạo tổng kết feedback cho toàn bộ phiên phỏng vấn</summary>
        Task<string> GenerateSessionSummaryAsync(List<InterviewResponseData> answeredResponses, double overallAvg);
    }
}