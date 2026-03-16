using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class Application2Params : QueryParameters
    {
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
        public int? UserId { get; set; }
        public ApplicationStatus? Status { get; set; }
        public ApplicationType? Type { get; set; }
    }
}
