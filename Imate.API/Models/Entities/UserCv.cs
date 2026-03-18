namespace Imate.API.Models.Entities
{
    public class UserCv
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTimeOffset UploadDate { get; set; }
        public string ScannedData { get; set; } = string.Empty;
        public string? AnalysisData { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
