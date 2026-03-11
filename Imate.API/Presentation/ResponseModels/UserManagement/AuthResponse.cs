namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public UserDto User { get; set; }
    }
    public class UserDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Subscription { get; set; }
        public string Role { get; set; }
        public bool? IsNewAccount { get; set; }
        public string? AccountStatus { get; set; }
        public string? VerificationStatus { get; set; }

    }
}
