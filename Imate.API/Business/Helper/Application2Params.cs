using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class Application2Params : QueryParameters
    {
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "desc"; // "asc" hoặc "desc"
        public int? UserId { get; set; }
        public string? Status { get; set; }
        public string? Type { get; set; }
    }
}
