using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Mật khẩu hiện tại không được để trống.")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới không được để trống.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Token xác thực không được để trống.")]
        public string FirebaseIdToken { get; set; }
    }
}
