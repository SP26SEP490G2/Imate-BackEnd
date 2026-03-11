using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Mentor
    {
        public int AccountId { get; set; }
        public string Bio { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateOnly? BirthDate { get; set; }
        public int Yoe { get; set; }
        public string? CvUrl { get; set; }
        public string? CertificateUrl { get; set; }
        public int PricePerSession { get; set; }
        public DateTimeOffset? PriceLastUpdatedDate { get; set; }
        public decimal? AvgRatings { get; set; }
        public int? TotalRatingCount { get; set; }
        public string BankAccountHolderName { get; set; } = string.Empty;
        public string BankAccountNumber { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;

        // Navigation properties
        public Account Account { get; set; } = null!;
        public ICollection<MentorSkill> MentorSkills { get; set; } = new List<MentorSkill>();
        public ICollection<MentorPosition> MentorPositions { get; set; } = new List<MentorPosition>();
        public ICollection<MentorCompany> MentorCompanies { get; set; } = new List<MentorCompany>();
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<MentorRecurringSlot> MentorRecurringSlots { get; set; } = new List<MentorRecurringSlot>();
    }
}
