namespace Imate.API.Models.Entities
{
    public class MentorRecurringSlot
    {
        public int Id { get; set; }
        public int MentorId { get; set; }
        public int SlotId { get; set; }
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Mentor Mentor { get; set; } = null!;
        public Slot Slot { get; set; } = null!;
    }
}
