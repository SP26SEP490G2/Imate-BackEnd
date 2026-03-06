using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Classification
{
    public class CreateCompanyRequestModel
    {
        [Required(ErrorMessage = "Tên công ty là bắt buộc.")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }
    }
}
