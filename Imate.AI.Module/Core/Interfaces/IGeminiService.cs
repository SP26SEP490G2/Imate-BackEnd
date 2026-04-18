using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Interface for Gemini AI service (Tầng 4 - AI Services)
    /// Chỉ chịu trách nhiệm gọi API bên ngoài (Beeknoee/Gemini)
    /// </summary>
    public interface IGeminiService
    {
        /// <summary>
        /// Gọi Gemini API với system prompt và user prompt
        /// </summary>
        Task<string> GenerateContentAsync(string systemPrompt, string userPrompt);

        /// <summary>
        /// Gọi Gemini API cho comment moderation (có timeout ngắn)
        /// </summary>
        Task<string> GenerateContentForCommentAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);

        /// <summary>
        /// Kiểm duyệt nội dung comment
        /// </summary>
        Task<CommentModerationResult> ModerateCommentAsync(string commentContent);
    }
}