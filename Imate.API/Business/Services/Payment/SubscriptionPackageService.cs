using System.Text.Json;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels.Payment;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.Payment
{
    public class SubscriptionPackageService : ISubscriptionPackageService
    {
        private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;
        private readonly ImateDbContext _context;

        public SubscriptionPackageService(
            ISubscriptionPackageRepository subscriptionPackageRepository,
            ImateDbContext context)
        {
            _subscriptionPackageRepository = subscriptionPackageRepository;
            _context = context;
        }

        public async Task<IEnumerable<SubscriptionPackageItemResponse>> GetPublicSubscriptionPackagesAsync()
        {
            var packages = await _subscriptionPackageRepository.GetActivePackagesOrderedByPriceAsync();

            return packages.Select(package => new SubscriptionPackageItemResponse(
                package.Id,
                package.Name,
                package.Price,
                FormatDuration(package.DurationDays),
                ParseBenefits(package.Benefits),
                package.IsRecommended,
                package.Rank
            ));
        }

        public async Task<SubscriptionOverviewResponse> GetSubscriptionOverviewAsync()
        {
            // 1. Total sold = count of UserSubscriptions
            var totalSold = await _context.UserSubscriptions.CountAsync();

            // 2. Total revenue = sum of completed subscription transactions
            var totalRevenue = await _context.Transactions
                .Where(t => t.TransactionType == TransactionType.Subscription
                          && t.Status == TransactionStatus.Completed)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            // 3. Featured package name (recommended or most sold)
            var featuredPackage = await _context.SubscriptionPackages
                .AsNoTracking()
                .Where(p => p.IsActive && p.IsRecommended)
                .Select(p => p.Name)
                .FirstOrDefaultAsync();

            // 4. Monthly sales grouped by package for the current year
            var currentYear = DateTime.UtcNow.Year;
            var monthlySalesRaw = await _context.UserSubscriptions
                .Where(us => us.CreatedAt.Year == currentYear)
                .Include(us => us.Package)
                .GroupBy(us => new { us.CreatedAt.Month, PackageName = us.Package.Name })
                .Select(g => new
                {
                    g.Key.Month,
                    g.Key.PackageName,
                    Count = g.Count()
                })
                .ToListAsync();

            // Build monthly sales list (12 months)
            var monthlySales = new List<MonthlySalesItem>();
            for (int month = 1; month <= 12; month++)
            {
                var monthData = monthlySalesRaw.Where(x => x.Month == month);
                var item = new MonthlySalesItem
                {
                    Month = month,
                    Year = currentYear,
                    PackageSales = monthData.ToDictionary(x => x.PackageName, x => x.Count)
                };
                monthlySales.Add(item);
            }

            return new SubscriptionOverviewResponse
            {
                TotalSold = totalSold,
                TotalRevenue = totalRevenue,
                FeaturedPackageName = featuredPackage,
                MonthlySales = monthlySales
            };
        }

        public async Task UpdatePackagePriceAsync(int packageId, decimal newPrice)
        {
            var package = await _subscriptionPackageRepository.GetByIdAsync(packageId);
            if (package == null)
            {
                throw new NotFoundException($"Không tìm thấy gói đăng ký với ID {packageId}");
            }

            package.Price = newPrice;
            await _subscriptionPackageRepository.UpdateAsync(package);
        }

        private static List<string> ParseBenefits(string? benefitsJson)
        {
            if (string.IsNullOrWhiteSpace(benefitsJson))
            {
                return new List<string>();
            }

            try
            {
                using var jsonDoc = JsonDocument.Parse(benefitsJson);
                
                if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (jsonDoc.RootElement.TryGetProperty("features", out var featuresElement) && 
                        featuresElement.ValueKind == JsonValueKind.Array)
                    {
                        var benefits = JsonSerializer.Deserialize<List<string>>(featuresElement.GetRawText());
                        return benefits ?? new List<string>();
                    }
                    return new List<string>();
                }
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var benefitsArray = JsonSerializer.Deserialize<List<string>>(benefitsJson);
                    return benefitsArray ?? new List<string>();
                }
                else if (jsonDoc.RootElement.ValueKind == JsonValueKind.String)
                {
                    var strVal = jsonDoc.RootElement.GetString();
                    return string.IsNullOrWhiteSpace(strVal) ? new List<string>() : new List<string> { strVal };
                }
                
                return new List<string> { benefitsJson };
            }
            catch (JsonException)
            {
                return new List<string> { benefitsJson };
            }
            catch (Exception ex)
            {
                throw new BadRequestException($"Benefits format không hợp lệ: {ex.Message}");
            }
        }

        private static string FormatDuration(int? durationDays)
        {
            if (!durationDays.HasValue)
            {
                return "Không giới hạn";
            }

            if (durationDays.Value <= 0)
            {
                throw new BadRequestException("DurationDays phải lớn hơn 0 khi được cung cấp.");
            }

            if (durationDays.Value % 30 == 0)
            {
                var months = durationDays.Value / 30;
                return months == 1 ? "1 tháng" : $"{months} tháng";
            }

            return durationDays.Value == 1 ? "1 ngày" : $"{durationDays.Value} ngày";
        }
    }
}
