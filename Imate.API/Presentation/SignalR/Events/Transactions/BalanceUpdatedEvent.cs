using MediatR;

namespace Imate.API.Presentation.SignalR.Events.Transactions
{
    public class BalanceUpdatedEvent : INotification
    {
        public int UserId { get; set; }
        public decimal? ImCoinBalance { get; set; }
        public int? AiCredit { get; set; }

        public BalanceUpdatedEvent(int userId, decimal imCoinBalance)
        {
            UserId = userId;
            ImCoinBalance = imCoinBalance;
            AiCredit = null;
        }

        public BalanceUpdatedEvent(int userId, int aiCredit)
        {
            UserId = userId;
            ImCoinBalance = null;
            AiCredit = aiCredit;
        }
        public BalanceUpdatedEvent(int userId, decimal imCoinBalance, int aiCredit)
        {
            UserId = userId;
            ImCoinBalance = imCoinBalance;
            AiCredit = aiCredit;
        }
    }
}
