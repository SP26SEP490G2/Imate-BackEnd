using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Transactions
{
    public class WithdrawalRequestCreateEvent : INotification
    {
        public Transaction Transaction { get; }

        public WithdrawalRequestCreateEvent(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}
