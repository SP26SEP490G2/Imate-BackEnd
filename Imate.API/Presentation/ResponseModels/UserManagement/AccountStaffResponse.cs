//using Amazon.S3.Model;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using ModelsEntities = Imate.API.Models.Entities;

namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class AccountStaffResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string? AvatarUrl { get; set; }
        public int QuestionCount { get; set; }
        public string Status { get; set; }
        public string RoleName { get; set; }
        public int ApplicationCount { get; set; }
        public int MentorCount { get; set; }
        public List<ModelsEntities.AuditLog> AuditLog { get; set; } = new List<ModelsEntities.AuditLog>();

    }
}
