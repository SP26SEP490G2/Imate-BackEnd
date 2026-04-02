using Imate.API.Business.Helper;
using Imate.API.Presentation.ResponseModels.Staff;

namespace Imate.API.Business.Interfaces.Staff
{
    public interface IStaffReviewService
    {
        Task<IEnumerable<StaffMentorApplicationResponse>> GetPendingMentorApplicationsAsync();
        Task<PagedList<StaffMentorApplicationResponse>> GetPendingMentorApplicationsPagedAsync(int pageNumber, int pageSize, string? searchTerm);
        Task<StaffMentorApplicationResponse?> GetMentorApplicationByIdAsync(int id);
        Task<IEnumerable<StaffRecruiterApplicationResponse>> GetPendingRecruiterApplicationsAsync();
        Task ReviewMentorApplicationAsync(int accountId, bool isApproved, string? note, int staffId);
        Task ReviewRecruiterApplicationAsync(int accountId, bool isApproved, string? note, int staffId, bool createCompany);
    }
}
