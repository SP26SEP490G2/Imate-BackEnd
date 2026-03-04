using Imate.API.Models.Enums;

namespace Imate.API.Business.Helper
{
    public class AccountParams: QueryParameters
    {
        public AccountStatus? AccountStatus { get; set; }
        public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
    }
}
