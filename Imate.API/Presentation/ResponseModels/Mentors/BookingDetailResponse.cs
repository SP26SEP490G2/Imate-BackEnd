using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Mentors
{
    public class BookingDetailResponse
    {
        public int BookingId { get; set; }
        public int MentorId { get; set; }
        public int CandidateId { get; set; }
        public string ProfileName { get; set; } = string.Empty;
        public string? ProfileAvatarUrl { get; set; }
        public string? JobTitle { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public DateOnly BookDate { get; set; }
        public BookingStatus Status { get; set; }
        public string? MeetingRoomId { get; set; }
        public string? AudioRecordKey { get; set; }
        public List<string> RecordingUrls { get; set; } = new List<string>();
        public int Price { get; set; }
    }
}
