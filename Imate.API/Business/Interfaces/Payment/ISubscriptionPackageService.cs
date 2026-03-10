using Imate.API.Presentation.ResponseModels.Payment;

namespace Imate.API.Business.Interfaces.Payment
{
    public interface ISubscriptionPackageService
    {
        Task<IEnumerable<SubscriptionPackageItemResponse>> GetPublicSubscriptionPackagesAsync();
        Task<SubscriptionOverviewResponse> GetSubscriptionOverviewAsync();
        Task UpdatePackagePriceAsync(int packageId, decimal newPrice);
    }
}
