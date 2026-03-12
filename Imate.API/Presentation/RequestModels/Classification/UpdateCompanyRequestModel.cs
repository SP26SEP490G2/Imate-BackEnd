using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Classification
{
    public class UpdateCompanyRequestModel
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc.")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public IFormFile? NewImageFile { get; set; }

        public bool IsActive { get; set; }
    }
}
