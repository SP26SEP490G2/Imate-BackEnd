using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Payment
{
    public class WithdrawRequest
    {
        [Required]
        public int Amount { get; set; }
        public string? BankCode { get; set; }
        public string? BankAccountHolder { get; set; }
        public string? BankAccountNumber { get; set; }
    }
}
