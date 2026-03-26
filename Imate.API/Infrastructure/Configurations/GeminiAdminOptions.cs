namespace Imate.API.Infrastructure.Configurations
{
    public class GeminiAdminOptions
    {
        public string? AdminApiKey { get; set; }
        public string? ProjectId { get; set; }
        public string? Location { get; set; } = "us-central1";
        public string? BillingAccountId { get; set; }
    }
}
