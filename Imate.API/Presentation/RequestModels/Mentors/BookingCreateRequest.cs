using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Mentors
{
    public class BookingCreateRequest
    {
        [Required]
        public int MentorId { get; set; }

        [Required]
        public int SlotId { get; set; }

        [Required]
        public DateOnly BookDate { get; set; }
    }
}
