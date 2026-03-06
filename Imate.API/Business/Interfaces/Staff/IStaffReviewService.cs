using Imate.API.Presentation.ResponseModels.Staff;

namespace Imate.API.Business.Interfaces.Staff
{
    public interface IStaffReviewService
    {
        Task<IEnumerable<StaffMentorApplicationResponse>> GetPendingMentorApplicationsAsync();
        Task<IEnumerable<StaffRecruiterApplicationResponse>> GetPendingRecruiterApplicationsAsync();
        Task ReviewMentorApplicationAsync(int accountId, bool isApproved, string? note, int staffId);
        Task ReviewRecruiterApplicationAsync(int accountId, bool isApproved, string? note, int staffId);
    }
}
