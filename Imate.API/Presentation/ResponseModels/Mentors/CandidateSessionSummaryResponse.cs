using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Mentors
{
    public class CandidateSessionSummaryResponse
    {
        public int BookingId { get; set; }
        public int MentorId { get; set; }
        public string MentorName { get; set; } = string.Empty;
        public string? MentorAvatarUrl { get; set; }
        public BookingStatus Status { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public int? RatingScore { get; set; }
        public string? ReviewText { get; set; }
        public DateTimeOffset? RatingCreatedAt { get; set; }
    }
}
