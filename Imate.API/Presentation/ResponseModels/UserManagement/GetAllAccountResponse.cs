using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class GetAllAccountResponse
    {

        public int Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string AvatarUrl { get; set; }
        public AccountStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; } 
        public DateTimeOffset? UpdatedAt { get; set; }

        // Chỉ hiển thị Tên Role (dạng string)
        public List<string> Roles { get; set; }     
       

    }
}
