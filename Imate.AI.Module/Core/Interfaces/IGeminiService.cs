using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Interface for Gemini AI service (Tầng 4 - AI Services)
    /// Chỉ chịu trách nhiệm gọi API bên ngoài (Beeknoee/Gemini)
    /// </summary>
    public interface IGeminiService
    {
        Task<string> GenerateContentAsync(string systemPrompt, string userPrompt);
        Task<string> GenerateContentForCvAnalysisAsync(string systemPrompt, string userPrompt);
        Task<string> GenerateContentForCommentAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
        Task<CommentModerationResult> ModerateCommentAsync(string commentContent);
    }
}