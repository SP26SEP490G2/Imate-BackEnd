using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class UpdateGeneralProfileRequest
    {
        [Required(ErrorMessage = "Họ và tên không được để trống.")]
        [StringLength(100)]
        public string FullName { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }
}
