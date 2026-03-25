namespace Imate.API.Business.Helper
{
    public class CommonParams : QueryParameters
    {
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
        public int? PositionId { get; set; } // Filter skills by position
        public string? SearchTerm { get; set; }
        public string? PositionName { get; set; }
        public string? SkillName { get; set; }
        public string? CompanyName { get; set; }
    }
}
