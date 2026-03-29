using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    /// <summary>
    ///  cập nhật hồ sơ Recruiter 
    /// </summary>
    public class UpdateRecruiterProfileRequest
    {
        [Required]
        public string CompanyName { get; set; }
        public IFormFile? CompanyLogo { get; set; }
        public string? Website { get; set; }
        [Required]
        public string? Industry { get; set; }
        [Required]
        public string? CompanySize { get; set; }
        [Required]
        public string? Address { get; set; }

        [Required]
        public string? Phone { get; set; }
    }
}
