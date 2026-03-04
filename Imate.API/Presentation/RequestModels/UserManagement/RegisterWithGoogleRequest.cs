namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class RegisterWithGoogleRequest
    {
        public string IdToken { get; set; }
        public string? Role { get; set; }
    }
}
