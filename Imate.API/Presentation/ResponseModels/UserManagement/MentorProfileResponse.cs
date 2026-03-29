namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class MentorProfileResponse : UserProfileResponse
    {
        public string Bio { get; set; }
        public string Phone { get; set; }
        public DateOnly? BirthDate { get; set; }
        public int Yoe { get; set; }
        public string? CvUrl { get; set; }
        public string? CertificateUrl { get; set; }
        public int PricePerSession { get; set; }
        public decimal? AvgRatings { get; set; }
        public int? TotalRatingCount { get; set; }
        public string BankAccountHolderName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankCode { get; set; }
        public IEnumerable<string> Skills { get; set; } = new List<string>();
        public IEnumerable<string> Positions { get; set; } = new List<string>();
        public IEnumerable<string> Companies { get; set; } = new List<string>();
        public string? VerificationStatus { get; set; }
    }
}
