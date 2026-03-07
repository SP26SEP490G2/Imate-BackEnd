using System.Text.Json;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.Presentation.ResponseModels.Payment;

namespace Imate.API.Business.Services.Payment
{
    public class SubscriptionPackageService : ISubscriptionPackageService
    {
        private readonly ISubscriptionPackageRepository _subscriptionPackageRepository;

        public SubscriptionPackageService(ISubscriptionPackageRepository subscriptionPackageRepository)
        {
            _subscriptionPackageRepository = subscriptionPackageRepository;
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
                package.IsRecommended
            ));
        }

        private static List<string> ParseBenefits(string? benefitsJson)
        {
            if (string.IsNullOrWhiteSpace(benefitsJson))
            {
                return new List<string>();
            }

            try
            {
                var benefits = JsonSerializer.Deserialize<List<string>>(benefitsJson);
                return benefits ?? new List<string>();
            }
            catch (JsonException ex)
            {
                throw new BadRequestException($"Benefits JSON không hợp lệ: {ex.Message}");
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
