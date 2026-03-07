namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class RecruiterProfileResponse : UserProfileResponse
    {
        public string CompanyName { get; set; }
        public string? CompanyLogo { get; set; }
        public string? Website { get; set; }
        public string? Industry { get; set; }
        public string? CompanySize { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? VerificationStatus { get; set; }
    }
}
