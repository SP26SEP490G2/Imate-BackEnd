using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels.Applications;
using Imate.API.Presentation.RequestModels.Applications;

namespace Imate.API.Business.Interfaces.Applications
{
    public interface IApplicationService
    {
        Task<PagedList<ApplicationListResponse>> GetApplicationsByIdAsync(int id, ApplicationParams appParams);
        Task<ApplicationDetailResponse> CreateTechnicalApplicationAsync(CreateTechnicalApplicationRequest request, int userId);
        Task<ApplicationDetailResponse> CreateReportApplicationAsync(CreateReportApplicationRequest request, int userId);
        Task<ApplicationDetailResponse> CreateReportCommentApplicationAsync(CreateReportCommentRequest request, int userId);
        Task<PagedList<object>> GetAllApplicationsAsync(Application2Params appParams);
        Task<object> GetApplicationDetails(int applicationId);
        Task<object> GetReportRatingDetails(int applicationId);
        Task<object> GetReportMentorDetails(int applicationId);
        Task<object> GetTechnicalDetails(int applicationId);

        Task ApproveApplicationAsync(int applicationId, int reviewerId, string? responseNote = null);
        Task RejectApplicationAsync(int applicationId, int reviewerId, string? responseNote = null);
        Task<IEnumerable<ApplicationNeedProcessSummaryResponse>> GetPendingSummaryAsync();
        Task<ReportCommentDetailResponse> GetReportCommentDetails(int applicationId);
    }
}
