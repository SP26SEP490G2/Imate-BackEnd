using Imate.API.Presentation.ResponseModels.Recruiter;

namespace Imate.API.Presentation.RequestModels.JobApplications
{
	public class JobPostingCandidateFilter
	{
		public string? SearchTerm { get; set; }
		public string? EmploymentType { get; set; }
		public string? Location { get; set; }
		public List<int>? SkillIds { get; set; }
		public List<int>? PositionIds { get; set; }
		public int PageNumber { get; set; } = 1;
		public int PageSize { get; set; } = 10;
	}

}
