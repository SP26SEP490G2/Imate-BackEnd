using Imate.API.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.Payment
{
    public class SubscriptionExpirationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionExpirationService> _logger;

        public SubscriptionExpirationService(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionExpirationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckExpiredSubscriptions();

                // Chạy 1 lần mỗi ngày
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }

        private async Task CheckExpiredSubscriptions()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                var expiredSubs = await unitOfWork.UserSubscriptions
                    .GetUserSubscriptions()
                    .Where(s => s.IsActive && s.EndDate < today)
                    .ToListAsync();

                foreach (var sub in expiredSubs)
                {
                    sub.IsActive = false;
                }

                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Expired {Count} subscriptions", expiredSubs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking expired subscriptions");
            }
        }
    }
}