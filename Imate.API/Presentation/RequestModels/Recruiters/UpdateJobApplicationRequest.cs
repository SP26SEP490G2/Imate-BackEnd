using System.Text.Json.Serialization;
using Imate.API.Models.Enums;

namespace Imate.API.Presentation.RequestModels.Recruiters
{
	public class UpdateJobApplicationRequest
	{
		public int Id { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public JobApplicationStatus Status { get; set; }
		public string? RecruiterFeedback { get; set; }
	}
}
