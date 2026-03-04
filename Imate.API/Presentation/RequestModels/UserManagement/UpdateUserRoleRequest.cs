using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class UpdateUserRoleRequest
    {
        [Required(ErrorMessage = "Role không được để trống.")]
        int a;
        public string Role { get; set; } = string.Empty;
    }
}

