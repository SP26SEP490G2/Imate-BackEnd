using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
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
            var systemConfigService = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISystemNotificationService>();

            try
            {
                int timeoutMinutes = await systemConfigService.GetDepositTimeoutMinutesAsync();
                var timeoutTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);

                var transactions = await unitOfWork.Transactions
                    .GetPendingTimeoutTransactions(timeoutTime);

                if (transactions.Count == 0) return;

                foreach (var tx in transactions)
                {
                    tx.Status = TransactionStatus.Failed;
                    tx.UpdatedAt = DateTimeOffset.UtcNow;
                    tx.Reason = $"Giao dịch tự động thất bại do quá {timeoutMinutes} phút không được xác nhận.";
                    await unitOfWork.Transactions.UpdateAsync(tx);
                    if (tx.TargetAccountId.HasValue)
                    {
                        await notificationService.CreateAndSendNotificationAsync(tx.TargetAccountId.Value, $"Giao dịch nạp {tx.Amount:N0} imCoin thất bại do quá {timeoutMinutes} phút không được xác nhận.", null);
                    }
                }

                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "[DepositTimeout] Đã tự động Failed {Count} giao dịch Deposit Pending quá {Minutes} phút",
                    transactions.Count, timeoutMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DepositTimeout] Lỗi khi kiểm tra timeout giao dịch Deposit");
            }
        }
    }
}