using Imate.AI.Module.Models.Responses;
using Microsoft.AspNetCore.Http;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Agent phân tích CV (Tầng 3 - Agents)
    /// Chịu trách nhiệm: build prompt, gọi AI Service, parse response
    /// </summary>
    public interface ICvAnalysisAgent
    {
        /// <summary>Phân tích CV bằng AI — build prompt + parse JSON response</summary>
        Task<CvAnalysisResponse> AnalyseCvAsync(string cvText);
    }
}