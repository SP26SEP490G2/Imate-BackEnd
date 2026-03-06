using Imate.API.Models.Entities;

namespace Imate.API.Presentation.ResponseModels.Staff
{
    public class StaffMentorApplicationResponse
    {
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public int Yoe { get; set; }
        public string? CvUrl { get; set; }
        public string? CertificateUrl { get; set; }
        public int PricePerSession { get; set; }
        public string BankAccountHolderName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public List<string> Skills { get; set; } = new();
        public List<string> Positions { get; set; } = new();
        public List<string> Companies { get; set; } = new();
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class StaffRecruiterApplicationResponse
    {
        public int AccountId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string? Website { get; set; }
        public string Industry { get; set; } = string.Empty;
        public string? CompanySize { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string VerificationStatus { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
    }
}
