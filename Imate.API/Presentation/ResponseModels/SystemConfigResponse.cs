namespace Imate.API.Presentation.ResponseModels
{
    public class SystemConfigResponse
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}

