namespace Imate.API.Business.Helper
{
    public class CommonParams : QueryParameters
    {
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
        public int? PositionId { get; set; } // Filter skills by position
    }
}
