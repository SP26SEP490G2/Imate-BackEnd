using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Payment
{
    public class DepositRequest
    {
        [Required]
        public int Amount { get; set; }
    }
}
