using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class RecruiterApplication
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string BusinessLicenseUrl { get; set; } = string.Empty;
        public ApplicationStatus Status { get; set; }
        public int? ReviewerId { get; set; }
        public string? Notes { get; set; }

        // Navigation properties
        public Account User { get; set; } = null!;
        public Account? Reviewer { get; set; }
    }
}
