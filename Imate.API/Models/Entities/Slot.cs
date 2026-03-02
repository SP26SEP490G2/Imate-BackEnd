namespace Imate.API.Models.Entities
{
    public class Slot
    {
        public int Id { get; set; }
        public int DayOfWeek { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }

        // Navigation properties
        public ICollection<MentorRecurringSlot> MentorRecurringSlots { get; set; } = new List<MentorRecurringSlot>();
    }
}
