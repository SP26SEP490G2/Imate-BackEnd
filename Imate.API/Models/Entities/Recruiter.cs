using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Recruiter
    {
        public int AccountId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string? Website { get; set; }
        public string Industry { get; set; } = string.Empty;
        public string? CompanySize { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public VerificationStatus VerificationStatus { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
    }
}
