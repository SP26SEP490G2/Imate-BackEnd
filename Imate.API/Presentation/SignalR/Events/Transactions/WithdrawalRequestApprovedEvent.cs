using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Transactions
{
    public class WithdrawalRequestApprovedEvent : INotification
    {
        public Transaction Transaction { get; }

        public WithdrawalRequestApprovedEvent(Transaction transaction)
        {
            Transaction = transaction;
        }
    }
}
