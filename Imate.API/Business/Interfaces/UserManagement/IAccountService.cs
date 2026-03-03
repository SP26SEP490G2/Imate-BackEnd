using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.UserManagement;

namespace Imate.API.Business.Interfaces.UserManagement
{
    public interface IAccountService
    {
        Task<bool> AreUsersExisted(int id);
        Task<PagedList<GetAllAccountResponse>> GetAllAccountAsync(AccountParams accountParams);
        Task<GetAllAccountResponse> GetAccountByIdAsync(int id);
        Task<Account> UpdateAccountStatusAsync(int id, string status);
        Task<UserProfileResponse> GetUserProfileAsync(int accountId,string subscription);
        Task UpdateGeneralProfileAsync(int accountId, UpdateGeneralProfileRequest request);
        Task<AccountStaffResponse> GetAccountDetailStaff(int accountId);
        Task<AccountMentorResponse> GetAccountDetailMentor(int accountId);
        Task<AccountCandidateResponse> GetAccountDetailCandidate(int accountId);
        Task<AccountDashboardResponseModel> GetAccountOverview();
        Task UpdateUserRoleAsync(int accountId, string role);
    }
}
