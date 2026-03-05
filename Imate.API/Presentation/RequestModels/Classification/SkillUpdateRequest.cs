using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Classification
{
    public class SkillUpdateRequest
    {
        [Required(ErrorMessage = "Không được để tên trống")]
        public string Name { get; set; }

        public bool IsActive { get; set; }
    }
}
