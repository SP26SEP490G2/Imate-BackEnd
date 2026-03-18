namespace Imate.AI.Module.Interfaces
{
    /// <summary>
    /// Interface for Gemini AI service
    /// </summary>
    public interface IGeminiService
    {
        /// <summary>
        /// Gọi Gemini API với system prompt và user prompt
        /// </summary>
        Task<string> GenerateContentAsync(string systemPrompt, string userPrompt);
    }
}
