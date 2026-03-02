using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Imate.API.BackgroundServices
{
    public class SubscriptionExpirationBackgroundService : BackgroundService
    {
        private readonly ILogger<SubscriptionExpirationBackgroundService> _logger;

        public SubscriptionExpirationBackgroundService(ILogger<SubscriptionExpirationBackgroundService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Expiration Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for expired subscriptions...");

                // Scan and process expired subscriptions
                // Ensure proper scope creation for EF Core contexts if needed

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
