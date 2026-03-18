using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;

namespace Imate.AI.Module.Interfaces
{
    /// <summary>
    /// Interface for CV Analysis using AI
    /// </summary>
    public interface ICvAnalysisService
    {
        /// <summary>
        /// Phân tích CV bằng Gemini AI
        /// </summary>
        Task<CvAnalysisResponse> AnalyseCvAsync(int accountId, AnalyseCvRequest request);
    }
}
