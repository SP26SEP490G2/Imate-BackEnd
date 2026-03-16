namespace Imate.API.Presentation.ResponseModels.Applications
{
    public class UpcomingBookingSummaryResponseModel
    {
        public int BookingId { get; set; }
        public string CandidateName { get; set; }
        public string MentorName { get; set; }
        public string? MentorAvatarUrl { get; set; }
        public DateTime StartTime { get; set; }
    }
}
