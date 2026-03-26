using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels.Payment;

namespace Imate.API.Business.Interfaces.Payment
{
    public interface IUserSubscriptionService
    {
        Task ActivateNewSubscriptionAsync(int accountId, int packageId);
        Task<UpgradePreviewResponse> GetUpgradePreviewAsync(int accountId, int newPackageId);
        Task CancelSubscriptionAsync(int accountId);
        Task<CancelPreviewResponse> GetCancelPreviewAsync(int accountId);
        Task<UserSubscriptionHistoryResponse> GetUserSubscriptionHistoryAsync(int accountId);
        Task<CurrentPackageResponse> GetCurrentPackageAsync(int accountId);
    }
}
