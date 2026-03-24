using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Mentors
{
    public class AddMentorRecurringSlotsRequest
    {
        [Required(ErrorMessage = "Danh sách SlotIds không được để trống")]
        [MinLength(1, ErrorMessage = "Phải có ít nhất 1 slot")]
        public List<int> SlotIds { get; set; } = new List<int>();
    }
}
