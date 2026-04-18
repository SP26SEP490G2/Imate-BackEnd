using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Agent tạo bài test luyện tập (Tầng 3 - Agents)
    /// Chịu trách nhiệm: build prompt, gọi AI Service, parse response
    /// </summary>
    public interface IPracticeTestAgent
    {
        /// <summary>Sinh bài test bằng AI — build prompt + parse JSON response</summary>
        Task<PracticeTestResponse> GenerateTestAsync(GeneratePracticeTestRequest request, string? cvContext, List<QuestionBankItem> ragQuestions);
    }
}