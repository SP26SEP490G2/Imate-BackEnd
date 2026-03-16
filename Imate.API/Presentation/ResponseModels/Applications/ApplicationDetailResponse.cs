namespace Imate.API.Presentation.ResponseModels.Applications
{
    public class ApplicationDetailResponse
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DateOnly DateSent { get; set; }
        public string Status { get; set; }
        public List<string>? Attachments { get; set; }
    }
}
