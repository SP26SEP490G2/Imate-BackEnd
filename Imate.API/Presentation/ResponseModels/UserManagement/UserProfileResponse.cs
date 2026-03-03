namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class UserProfileResponse
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Subscription { get; set; }
        public int Balance { get; set; }
        public string Role { get; set; }
        public string? AccountStatus { get; set; }
    }
}
