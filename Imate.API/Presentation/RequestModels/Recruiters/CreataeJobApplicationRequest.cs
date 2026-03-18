using System.Text.Json.Serialization;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.RequestModels.Recruiters
{
	public class CreataeJobApplicationRequest
	{
		public int Id { get; set; }
		public int JobId { get; set; }
		public int CandidateId { get; set; }
		public int CvId { get; set; }
		public DateTimeOffset AppliedDate { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public JobApplicationStatus Status { get; set; }
		public string? RecruiterFeedback { get; set; }

		// Navigation properties
		public Job Job { get; set; } = null!;
		public Account Candidate { get; set; } = null!;
	}
}
