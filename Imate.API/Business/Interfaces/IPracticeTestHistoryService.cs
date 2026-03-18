using Imate.API.Presentation.RequestModels.PracticeTest;
using Imate.API.Presentation.ResponseModels.PracticeTest;

namespace Imate.API.Business.Interfaces
{
    public interface IPracticeTestHistoryService
    {
        Task<int> SubmitTestAsync(int accountId, SubmitPracticeTestRequest request);
        Task<List<TestHistoryItemResponse>> GetHistoryAsync(int accountId);
        Task<TestHistoryDetailResponse> GetDetailAsync(int accountId, int sessionId);
    }
}
