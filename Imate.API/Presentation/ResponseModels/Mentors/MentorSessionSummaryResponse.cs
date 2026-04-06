using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Mentors
{
    public class MentorSessionSummaryResponse
    {
        public int BookingId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string? CandidateAvatarUrl { get; set; }
        public BookingStatus Status { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public int? RatingScore { get; set; }
        public string? ReviewText { get; set; }
    }
}
