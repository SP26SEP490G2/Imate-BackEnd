using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class TransactionResponse
    {
        public int TransactionId { get; set; }
        public DateTimeOffset Date { get; set; }
        public int Amount { get; set; }
        public string TransactionType { get; set; } // "DEPOSIT", "WITHDRAWAL"
        public string Status { get; set; } // "SUCCESS", "PENDING"
        public string? ExternalCode { get; set; }
        public string? Reason { get; set; }
        public WithdrawalDetailDto? WithdrawalDetail { get; set; }
        public string? SourceAccountName { get; set; }
        public string? TargetAccountName { get; set; }
        public int? BookingId { get; set; }
        public DateTimeOffset? EscrowDeadline { get; set; }
        public decimal? CommissionRateApplied { get; set; }
        public bool? HasPendingMentorReport { get; set; }
        // Lợi nhuận cho từng giao dịch payout của mentor: Fee - Payout (>=0)
        public int? Profit { get; set; }
    }

    public class WithdrawalDetailDto
    {
        public string BankCode { get; set; }
        public string BankAccountHolder { get; set; }
        public string BankAccountNumber { get; set; }
    }
}
