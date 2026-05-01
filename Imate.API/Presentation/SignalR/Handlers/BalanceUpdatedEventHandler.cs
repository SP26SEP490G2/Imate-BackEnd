using Imate.API.Presentation.SignalR.Events.Transactions;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Imate.API.Presentation.SignalR.Handlers
{
    public class BalanceUpdatedEventHandler : INotificationHandler<BalanceUpdatedEvent>
    {
        private readonly IHubContext<BalanceHub> _hubContext;
        private readonly ILogger<BalanceUpdatedEventHandler> _logger;

        public BalanceUpdatedEventHandler(
            IHubContext<BalanceHub> hubContext,
            ILogger<BalanceUpdatedEventHandler> logger)
        {
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task Handle(BalanceUpdatedEvent notification, CancellationToken cancellationToken)
        {
            try
            {
                var groupId = notification.UserId.ToString();

                if (notification.ImCoinBalance.HasValue && notification.AiCredit.HasValue)
                {
                    await _hubContext.Clients.Group(groupId).SendAsync(
                        "BalanceAndAiCreditUpdated",
                        new { imCoinBalance = notification.ImCoinBalance.Value, aiCredit = notification.AiCredit.Value },
                        cancellationToken: cancellationToken);
                }
                else if (notification.ImCoinBalance.HasValue)
                {
                    await _hubContext.Clients.Group(groupId).SendAsync(
                        "BalanceUpdated",
                        new { imCoinBalance = notification.ImCoinBalance.Value },
                        cancellationToken: cancellationToken);
                }
                else if (notification.AiCredit.HasValue)
                {
                    await _hubContext.Clients.Group(groupId).SendAsync(
                        "AiCreditUpdated",
                        new { aiCredit = notification.AiCredit.Value },
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[BalanceUpdatedEventHandler] Error sending balance update");
            }
        }
    }
}
