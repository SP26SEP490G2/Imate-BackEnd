using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class LoginRequest
    {
        [Required]
        public string FirebaseIdToken { get; set; }
    }
}
