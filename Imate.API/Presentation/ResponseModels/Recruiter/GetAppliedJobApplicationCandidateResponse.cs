using System.Text.Json.Serialization;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.Recruiter
{
	public class GetAppliedJobApplicationCandidateResponse
	{
		public int ApplicationId { get; set; }
		public DateTimeOffset AppliedDate { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]

		public JobApplicationStatus Status { get; set; }
		public string? RecruiterFeedback { get; set; }

		public int CandidateId { get; set; }
		public string CandidateFullName { get; set; }
		public string CandidateEmail { get; set; }

		public string CandidateFileUrl { get; set; } = string.Empty;
		public string CandidateFileName { get; set; } = string.Empty;
		public string CandidateScannedData { get; set; } = string.Empty;

	}
}
