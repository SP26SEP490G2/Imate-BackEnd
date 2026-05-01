using Imate.API.Presentation.ResponseModels.Payment;

namespace Imate.API.Business.Interfaces.Payment
{
    public interface ISubscriptionPackageService
    {
        Task<IEnumerable<SubscriptionPackageItemResponse>> GetPublicSubscriptionPackagesAsync();
        Task<SubscriptionOverviewResponse> GetSubscriptionOverviewAsync();
        Task UpdatePackagePriceAsync(int packageId, decimal newPrice);
        Task UpdatePackageBenefitsAsync(int packageId, List<string> benefits);
        Task UpdatePackageNameAsync(int packageId, string name);
        Task<SubscriptionPackageItemResponse> CreatePackageAsync(string name, decimal price, int durationDays, List<string> benefits, bool isRecommended);
        Task DeactivatePackageAsync(int packageId);
    }
}
