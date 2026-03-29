using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Recruiters
{
    public class SubmitRecruiterProfileRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyAddress { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyLogo { get; set; }
        [Required(ErrorMessage = "Số điện thoại không được để trống.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Số điện thoại chỉ được chứa ký tự số.")]
        public string Phone { get; set; } = string.Empty;
        
        // Optional original fields if still needed
        public string? Industry { get; set; }
        public string? CompanySize { get; set; }
    }
}
