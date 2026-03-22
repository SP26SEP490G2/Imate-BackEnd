using Imate.API.Models.Enums;
using System.Text.Json.Serialization;

namespace Imate.API.Presentation.ResponseModels.JobApplications
{
	public class GetCandidateAppliedJobResponse
	{
		public int Id { get; set; }
		public string Title { get; set; } = string.Empty;
		public string CompanyName { get; set; } = string.Empty;
		public string? CompanyLogo { get; set; }
		public string EmploymentType { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public long MinSalary { get; set; }
		public long MaxSalary { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public JobApplicationStatus Status { get; set; }
		public DateTimeOffset AppliedDate { get; set; }
		public string? Feedback {  get; set; }
	}
}

