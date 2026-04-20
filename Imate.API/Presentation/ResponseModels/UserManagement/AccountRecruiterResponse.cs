namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class AccountRecruiterResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public string Status { get; set; }
        public string RoleName { get; set; }
        public string CompanyName { get; set; }
        public string? CompanyLogo { get; set; }
        public string? Website { get; set; }
        public string Industry { get; set; }
        public string? CompanySize { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? VerificationStatus { get; set; }
        public int JobPostCount { get; set; }
    }
}
