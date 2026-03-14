using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;

namespace Imate.AI.Module.Interfaces
{
    /// <summary>
    /// Interface cho Practice Test Service
    /// UC-30: Practice Test
    /// </summary>
    public interface IPracticeTestService
    {
        /// <summary>
        /// Sinh bài test luyện tập bằng AI dựa trên loại test, lĩnh vực, cấp bậc
        /// </summary>
        Task<PracticeTestResponse> GenerateTestAsync(int accountId, GeneratePracticeTestRequest request);
    }
}
