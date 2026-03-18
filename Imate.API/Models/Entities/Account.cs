using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Account
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public LoginProvider Provider { get; set; }
        public string? ProviderId { get; set; }
        public int Balance { get; set; }
        public AccountStatus Status { get; set; }
        public int FreeUsedMock { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Mentor? Mentor { get; set; }
        public Recruiter? Recruiter { get; set; }
        public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
        public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<SavedQuestion> SavedQuestions { get; set; } = new List<SavedQuestion>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Vote> Votes { get; set; } = new List<Vote>();
        public ICollection<Booking> CandidateBookings { get; set; } = new List<Booking>();
        public ICollection<Transaction> SourceTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> TargetTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReviewedTransactions { get; set; } = new List<Transaction>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
        public ICollection<Application> ReviewedApplications { get; set; } = new List<Application>();
        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
        public ICollection<UserCv> UserCvs { get; set; } = new List<UserCv>();
        public ICollection<InterviewSession> InterviewSessions { get; set; } = new List<InterviewSession>();
        public ICollection<PracticeTestSession> PracticeTestSessions { get; set; } = new List<PracticeTestSession>();
        public ICollection<SystemNotification> ReceivedNotifications { get; set; } = new List<SystemNotification>();
        public ICollection<SystemNotification> TriggeredNotifications { get; set; } = new List<SystemNotification>();
        public ICollection<Job> PostedJobs { get; set; } = new List<Job>();
        public ICollection<JobApplication> CandidateJobApplications { get; set; } = new List<JobApplication>();
        public ICollection<RecruiterApplication> RecruiterApplications { get; set; } = new List<RecruiterApplication>();
        public ICollection<RecruiterApplication> ReviewedRecruiterApplications { get; set; } = new List<RecruiterApplication>();
    }
}
