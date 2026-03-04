using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels.Mentors;

namespace Imate.API.Presentation.ResponseModels.UserManagement
{
    public class AccountMentorResponse
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string Bio { get; set; }
        public decimal? AvgRatings { get; set; }
        public int PricePerSession { get; set; }
        public string Status { get; set; }
        public string RoleName { get; set; }
        public int TotalCompletedSessions { get; set; }
        public List<ReviewResponseModel> Reviews { get; set; }

    }
}
