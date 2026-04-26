using Imate.API.Models.Entities;
using Imate.API.Models.Enums;

namespace Imate.API.Entities
{
    public class TrainingJourney
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int UserCvId { get; set; }
        public string JobDescriptionText { get; set; } = string.Empty;
        public string GapsJson { get; set; } = "[]";
        public TrainingStatus Status { get; set; }
        public int TotalSessions { get; set; } = 0;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? Name { get; set; } = "Lộ trình không tên";
        public string ProfileGapsJson { get; set; } = "[]";
        public string? PositionName { get; set; }
        public string? SkillName { get; set; }
        public string? LevelName { get; set; }
        public string? CompanyName { get; set; }

        public virtual UserCv? UserCv { get; set; }
        public virtual ICollection<InterviewSession> Sessions { get; set; } = new List<InterviewSession>();

    }
}