using MediatR;
using Imate.API.Models.Entities;

namespace Imate.API.Presentation.SignalR.Events.Transactions
{
    /// <summary>
    /// Event được publish khi booking payout được xử lý thành công.
    /// </summary>
    public class BookingPayoutProcessedEvent : INotification
    {
        public Transaction PayoutTransaction { get; }
        public int PayoutAmount { get; }
        public int BookingId { get; }

        public BookingPayoutProcessedEvent(
            Transaction payoutTransaction,
            int payoutAmount,
            int bookingId)
        {
            PayoutTransaction = payoutTransaction;
            PayoutAmount = payoutAmount;
            BookingId = bookingId;
        }
    }
}

