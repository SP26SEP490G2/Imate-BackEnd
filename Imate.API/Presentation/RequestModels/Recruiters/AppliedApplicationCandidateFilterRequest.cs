using Imate.API.Models.Enums;

namespace Imate.API.Presentation.RequestModels.Recruiters
{
	public class AppliedApplicationCandidateFilterRequest
	{
		public string? SearchTerm { get; set; }
		public JobApplicationStatus? Status { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 10;
	}
}
