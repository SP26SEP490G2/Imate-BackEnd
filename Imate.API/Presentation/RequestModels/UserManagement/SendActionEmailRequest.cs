namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class SendActionEmailRequest
    {
        public string OobCode { get; set; }
        public string Email { get; set; }
        public string ActionType { get; set; } // "VERIFY_EMAIL" or "PASSWORD_RESET"
    }
}

