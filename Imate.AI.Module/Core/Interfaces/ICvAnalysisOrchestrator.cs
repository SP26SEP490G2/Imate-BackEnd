using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.AspNetCore.Http;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Orchestrator phân tích CV (Tầng 2 - Orchestrators)
    /// Điều phối workflow: cache check + data access + Agent
    /// </summary>
    public interface ICvAnalysisOrchestrator
    {
        /// <summary>Phân tích CV — check cache, gọi Agent, save cache</summary>
        Task<CvAnalysisResponse> AnalyseCvAsync(int accountId, AnalyseCvRequest request);
        Task ValidateCvIsItAsync(IFormFile file);
    }
}