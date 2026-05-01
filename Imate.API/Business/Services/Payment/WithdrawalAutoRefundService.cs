using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using Imate.API.Presentation.SignalR.Events.Transactions;
using MediatR;

namespace Imate.API.Business.Services.Payment
{
    public class WithdrawalAutoRefundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WithdrawalAutoRefundService> _logger;

        public WithdrawalAutoRefundService(
            IServiceScopeFactory scopeFactory,
            ILogger<WithdrawalAutoRefundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        // Helper method to resolve IMediator from the service scope
        private IMediator GetMediator(IServiceProvider serviceProvider)
        {
            return serviceProvider.GetRequiredService<IMediator>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessExpiredWithdrawals();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessExpiredWithdrawals()
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var systemConfigService = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<ISystemNotificationService>();
            var mediator = GetMediator(scope.ServiceProvider);

            try
            {
                int autoRefundHours = await systemConfigService.GetWithdrawalAutoRefundHoursAsync();
                var cutoffTime = DateTime.UtcNow.AddHours(-autoRefundHours);

                var expiredWithdrawals = await unitOfWork.Transactions
                    .GetPendingWithdrawalTimeoutTransactions(cutoffTime);

                if (expiredWithdrawals.Count == 0) return;

                _logger.LogInformation(
                    "[WithdrawalAutoRefund] Phát hiện {Count} giao dịch Withdrawal Pending quá {Hours} giờ. Bắt đầu hoàn tiền...",
                    expiredWithdrawals.Count, autoRefundHours);

                foreach (var tx in expiredWithdrawals)
                {
                    try
                    {
                        // Hoàn tiền về ví người dùng
                        if (tx.SourceAccountId.HasValue)
                        {
                            var account = await unitOfWork.Accounts.GetByIdAsync(tx.SourceAccountId.Value);
                            if (account != null)
                            {
                                account.Balance += tx.Amount;
                                account.UpdatedAt = DateTimeOffset.UtcNow;
                                await unitOfWork.Accounts.UpdateAsync(account);

                                _logger.LogInformation(
                                    "[WithdrawalAutoRefund] Đã hoàn {Amount} vào ví account {AccountId} (Transaction #{TxId})",
                                    tx.Amount, account.Id, tx.Id);
                                await notificationService.CreateAndSendNotificationAsync(tx.SourceAccountId.Value, $"Yêu cầu rút {tx.Amount:N0} imCoin bị hủy do chưa được duyệt sau {autoRefundHours} giờ. imCoin đã được hoàn lại vào ví.", null);

                                // Phát event SignalR để cập nhật balance realtime
                                var balanceEvent = new BalanceUpdatedEvent(account.Id, account.Balance);
                                await mediator.Publish(balanceEvent);
                                _logger.LogInformation("Published BalanceUpdatedEvent for auto-refund - accountId={AccountId}, newBalance={Balance}", account.Id, account.Balance);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "[WithdrawalAutoRefund] Không tìm thấy account {AccountId} để hoàn tiền cho Transaction #{TxId}",
                                    tx.SourceAccountId.Value, tx.Id);
                            }
                        }
                        tx.Status = TransactionStatus.Failed;
                        tx.UpdatedAt = DateTimeOffset.UtcNow;
                        tx.Reason = $"Yêu cầu rút tiền bị hủy do chưa được duyệt sau {autoRefundHours} giờ. imCoin đã được hoàn lại vào ví.";
                        await unitOfWork.Transactions.UpdateAsync(tx);
                    }
                    catch (Exception exInner)
                    {
                        _logger.LogError(exInner,
                            "[WithdrawalAutoRefund] Lỗi khi xử lý hoàn tiền cho Transaction #{TxId}", tx.Id);
                        // Tiếp tục với transaction kế tiếp, không dừng toàn bộ batch
                    }
                }

                await unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "[WithdrawalAutoRefund] Hoàn thành: đã xử lý {Count} giao dịch Withdrawal hết hạn",
                    expiredWithdrawals.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WithdrawalAutoRefund] Lỗi khi xử lý tự động hoàn tiền Withdrawal");
            }
        }
    }
}
