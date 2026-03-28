using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class ApplicationParams : QueryParameters
    {
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
        public int? ReviewId { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }

    }
}
