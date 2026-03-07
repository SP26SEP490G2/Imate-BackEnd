namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class RegisterWithEmailRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }
}
