namespace Imate.API.Models.Entities
{
    public class WithdrawalDetail
    {
        public int TransactionId { get; set; }
        public string BankCode { get; set; } = string.Empty;
        public string BankAccountHolder { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;

        // Navigation properties
        public Transaction Transaction { get; set; } = null!;
    }
}
