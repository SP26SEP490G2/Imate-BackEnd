using Imate.API.Models.Enums;

namespace Imate.API.Models.Entities
{
    public class Job
    {
        public int Id { get; set; }
        public int RecruiterId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string EmploymentType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long MinSalary { get; set; }
        public long MaxSalary { get; set; }
        public DateTimeOffset ApplicationDeadline { get; set; }
        public JobStatus Status { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }

        // Navigation properties
        public Account Recruiter { get; set; } = null!;
        public ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
        public ICollection<JobPosition> JobPositions { get; set; } = new List<JobPosition>();
        public ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
