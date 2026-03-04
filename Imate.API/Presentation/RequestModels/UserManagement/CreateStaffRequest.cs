using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.UserManagement
{
    public class CreateEmployeeRequest
    {
        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, MinLength(2)]
        public string FullName { get; set; }
    }
}
