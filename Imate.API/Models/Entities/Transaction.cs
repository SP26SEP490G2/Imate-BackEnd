using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public int? SourceAccountId { get; set; }
        public int? TargetAccountId { get; set; }
        public TransactionType TransactionType { get; set; }
        public int Amount { get; set; }
        public decimal? CommissionRateApplied { get; set; }
        public int? BookingId { get; set; }
        public int? UserSubscriptionId { get; set; }
        public int? ApplicationId { get; set; }
        public TransactionStatus Status { get; set; }
        public DateTimeOffset? EscrowDeadline { get; set; }
        public string? ExternalTransactionCode { get; set; }
        public int? ReviewerId { get; set; }
        public string? Reason { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Account? SourceAccount { get; set; }
        public Account? TargetAccount { get; set; }
        public Account? Reviewer { get; set; }
        public Booking? Booking { get; set; }
        public UserSubscription? UserSubscription { get; set; }
        public Application? Application { get; set; }
        public WithdrawalDetail? WithdrawalDetail { get; set; }
    }
}
