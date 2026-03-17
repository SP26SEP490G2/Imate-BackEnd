using Imate.API.Models.Enums;
using Imate.API.Presentation.ResponseModels.Recruiter;
using System.Text.Json.Serialization;

namespace Imate.API.Presentation.ResponseModels.JobApplications
{
	public class GetAllOpenedJobResponse
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string JobDescription { get; set; } = string.Empty;
		public string EmploymentType { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public long MinSalary { get; set; }
		public long MaxSalary { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public JobStatus Status { get; set; }
		public DateTimeOffset ApplicationDeadline { get; set; }
		public required ComapnyRecruitment CompanyRecruiter { get; set;}
		public ICollection<JobSkillResponse> JobSkills { get; set; } = new List<JobSkillResponse>();
		public ICollection<JobPositionResponse> JobPositions { get; set; } = new List<JobPositionResponse>();
	}

	public class ComapnyRecruitment
	{
		public string Email { get; set; } = string.Empty;
		public string CompanyName { get; set; } = string.Empty;
		public string? CompanyLogo { get; set; }
		public string? Website { get; set; }
		public string Industry { get; set; } = string.Empty;
		public string? CompanySize { get; set; }
		public string? Address { get; set; }
		public string? Phone { get; set; }
	}
}
