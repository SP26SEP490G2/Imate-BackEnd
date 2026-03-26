using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;

namespace Imate.API.Business.Services.Payment
{
    public class TransactionTimeoutService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TransactionTimeoutService> _logger;

        public TransactionTimeoutService(
            IServiceScopeFactory scopeFactory,
            ILogger<TransactionTimeoutService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await CheckTimeoutTransactions();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckTimeoutTransactions()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            try
            {
                var timeoutTime = DateTime.UtcNow.AddMinutes(-1);

                var transactions = await unitOfWork.Transactions
                    .GetPendingTimeoutTransactions(timeoutTime);

                foreach (var tx in transactions)
                {
                    tx.Status = TransactionStatus.Failed;
                    await unitOfWork.Transactions.UpdateAsync(tx);
                }

                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Timeout {Count} transactions", transactions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking timeout transactions");
            }
        }
    }
}
