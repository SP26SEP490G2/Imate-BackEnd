using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class JobApplication
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int CandidateId { get; set; }
        public int CvId { get; set; }
        public DateTimeOffset AppliedDate { get; set; }
        public JobApplicationStatus Status { get; set; }
        public string? RecruiterFeedback { get; set; }

        // Navigation properties
        public Job Job { get; set; } = null!;
        public Account Candidate { get; set; } = null!;
        public UserCv Cv { get; set; } = null!;
    }
}
