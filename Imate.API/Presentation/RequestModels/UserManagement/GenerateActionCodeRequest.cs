namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class GenerateActionCodeRequest
    {
        public string Email { get; set; }
        public string ActionType { get; set; } // "VERIFY_EMAIL" or "PASSWORD_RESET"
    }
}

