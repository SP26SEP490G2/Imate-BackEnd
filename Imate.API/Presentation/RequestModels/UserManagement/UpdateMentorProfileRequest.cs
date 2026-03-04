using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class UpdateMentorProfileRequest
    {
        [Required]
        public string Bio { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        [Range(0, int.MaxValue)]
        public int? PricePerSession { get; set; }

        [Required]
        public string BankAccountHolderName { get; set; }

        [Required]
        public string BankAccountNumber { get; set; }

        [Required]
        public string BankCode { get; set; }
    }
}
