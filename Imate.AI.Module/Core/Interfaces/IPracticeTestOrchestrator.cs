using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Orchestrator bài test luyện tập (Tầng 2 - Orchestrators)
    /// Điều phối workflow: validate + RAG query + Agent
    /// </summary>
    public interface IPracticeTestOrchestrator
    {
        /// <summary>Sinh bài test — validate, RAG, gọi Agent</summary>
        Task<PracticeTestResponse> GenerateTestAsync(int accountId, GeneratePracticeTestRequest request);
    }
}