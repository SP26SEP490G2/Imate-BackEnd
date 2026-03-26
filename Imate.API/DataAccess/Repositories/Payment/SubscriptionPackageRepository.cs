using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Payment
{
    public class SubscriptionPackageRepository : ISubscriptionPackageRepository
    {
        private readonly ImateDbContext _context;

        public SubscriptionPackageRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPackage>> GetActivePackagesOrderedByPriceAsync()
        {
            return await _context.SubscriptionPackages
                .AsNoTracking()
                .Where(package => package.IsActive)
                .OrderBy(package => package.Price)
                .ToListAsync();
        }

        public async Task<SubscriptionPackage?> GetByIdAsync(int id)
        {
            return await _context.SubscriptionPackages.FindAsync(id);
        }

        public async Task UpdateAsync(SubscriptionPackage package)
        {
            _context.SubscriptionPackages.Update(package);
            await _context.SaveChangesAsync();
        }

        public async Task<SubscriptionPackage> GetSubscriptionPackageByIdAsync(int id)
        {
            return await _context.SubscriptionPackages.FindAsync(id);
        }

        public async Task<SubscriptionPackage> GetLowestRankPackageAsync()
        {
            return await _context.SubscriptionPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.Rank)
                .FirstAsync();
        }
    }
}

