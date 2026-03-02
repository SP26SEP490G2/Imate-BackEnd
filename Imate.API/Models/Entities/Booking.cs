using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Booking
    {
        public int Id { get; set; }
        public int CandidateId { get; set; }
        public int MentorId { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateOnly BookDate { get; set; }
        public int PriceAtBooking { get; set; }
        public BookingStatus Status { get; set; }
        public string AgoraChannelName { get; set; } = string.Empty;
        public string? AudioRecordKey { get; set; }
        public int? RatingScore { get; set; }
        public string? ReviewText { get; set; }
        public DateTimeOffset? RatingCreatedAt { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Account Candidate { get; set; } = null!;
        public Mentor Mentor { get; set; } = null!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
