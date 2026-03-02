using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels
{
    public class AccountRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;
    }
}
