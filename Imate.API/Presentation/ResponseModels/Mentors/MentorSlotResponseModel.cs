using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Mentors
{
    public class MentorRecurringSlotsResponse
    {
        public int MentorId { get; set; }
        public List<SlotsByDayResponse> SlotsByDay { get; set; } = new();
    }

    public class SlotsByDayResponse
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public List<MentorSlotDetailResponse> Slots { get; set; } = new();
    }

    public class MentorSlotDetailResponse
    {
        public int Id { get; set; }
        public int MentorId { get; set; }
        public int SlotId { get; set; }
        public SlotDetailResponse Slot { get; set; } = null!;
        public bool? IsBooked { get; set; } = false;
    }

    public class SlotDetailResponse
    {
        public int Id { get; set; }
        public int DayOfWeek { get; set; }
        public string DayOfWeekName { get; set; } = string.Empty;
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string DisplayTime => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
    }

    public class MentorBookedSlotResponse
    {
        public int BookingId { get; set; }
        public int CandidateId { get; set; }
        public string CandidateName { get; set; } = string.Empty;
        public string? CandidateAvatarUrl { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateOnly BookDate { get; set; }
        public BookingStatus Status { get; set; }
    }
}
