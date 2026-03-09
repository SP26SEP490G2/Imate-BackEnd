namespace Imate.API.Presentation.RequestModels.Recruiters
{
    public class RecruiterJobSearchFilterRequest
    {
        public string? SearchTerm { get; set; }
        public string? Location { get; set; }
        public string? EmploymentType { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
