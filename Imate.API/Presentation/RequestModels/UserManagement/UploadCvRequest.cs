using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class UploadCvRequestModel
    {
        [Required(ErrorMessage = "Tên CV không được để trống.")]
        [StringLength(255, ErrorMessage = "Tên CV không được vượt quá 255 ký tự.")]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "File CV không được để trống.")]
        public IFormFile File { get; set; } = null!;
    }
}

