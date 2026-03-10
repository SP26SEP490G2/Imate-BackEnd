using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Payment
{
    public interface ISubscriptionPackageRepository
    {
        Task<IEnumerable<SubscriptionPackage>> GetActivePackagesOrderedByPriceAsync();
        Task<SubscriptionPackage?> GetByIdAsync(int id);
        Task UpdateAsync(SubscriptionPackage package);
    }
}
