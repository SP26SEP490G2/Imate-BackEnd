namespace Imate.API.Models.Entities
{
    public class UserSubscription
    {
        public int Id { get; set; }
        public int CandidateId { get; set; }
        public int PackageId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public int InitialMockLimit { get; set; }
        public int MockInterviewUsed { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Account Candidate { get; set; } = null!;
        public SubscriptionPackage Package { get; set; } = null!;
        public virtual Transaction Transaction { get; set; }
    }
}
