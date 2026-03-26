using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Transactions
{
    public class WithdrawalRequestRejectedEvent : INotification
    {
        public Transaction Transaction { get; }

        public WithdrawalRequestRejectedEvent(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}
