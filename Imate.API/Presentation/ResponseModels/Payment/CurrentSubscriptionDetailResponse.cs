namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class CurrentSubscriptionDetailResponse
    {
        public string PackageName { get; set; }
        public int Rank { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? RemainingDays { get; set; }
        public bool IsExpired { get; set; }
        public int MockInterviewUsed { get; set; }
        public int InitialMockLimit { get; set; }
    }
}