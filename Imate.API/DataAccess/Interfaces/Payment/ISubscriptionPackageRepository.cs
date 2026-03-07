using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Payment
{
    public interface ISubscriptionPackageRepository
    {
        Task<IEnumerable<SubscriptionPackage>> GetActivePackagesOrderedByPriceAsync();
    }
}
