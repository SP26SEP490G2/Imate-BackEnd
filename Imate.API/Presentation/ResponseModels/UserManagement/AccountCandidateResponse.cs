namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class AccountCandidateResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string Status { get; set; }
        public string RoleName { get; set; }
        public List<string> ExPackages { get; set; }
        public string PresentPackage { get; set; }
        public int MentorSessionCount { get; set; }
    }
}
