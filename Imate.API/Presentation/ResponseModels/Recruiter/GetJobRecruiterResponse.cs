using System.Text.Json.Serialization;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Recruiter
{

    public class GetJobRecruiterResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string JobDescription { get; set; } = string.Empty;
        public string EmploymentType { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public long MinSalary { get; set; }
        public long MaxSalary { get; set; }
        public DateTimeOffset ApplicationDeadline { get; set; }
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public JobStatus Status { get; set; }
        public ICollection<JobSkillResponse> JobSkills { get; set; } = new List<JobSkillResponse>();
        public ICollection<JobPositionResponse> JobPositions { get; set; } = new List<JobPositionResponse>();
    }

    public class JobSkillResponse
    {
        public int Id { get; set; }
        public string SkillName { get; set; } = string.Empty;
    }

    public class JobPositionResponse
    {
        public int Id { get; set; }
        public string PositionName { get; set; } = string.Empty;
    }

}
