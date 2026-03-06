using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.ResponseModels.Classification
{
    public class CompanyListRequestModel
    {
        [Range(1, int.MaxValue, ErrorMessage = "PageNumber phải lớn hơn 0.")]
        public int PageNumber { get; set; } = 1;

        [Range(1, 100, ErrorMessage = "PageSize phải từ 1 đến 100.")]
        public int PageSize { get; set; } = 10;

        public string? SearchTerm { get; set; }
        public bool? IsActive { get; set; }

        // Sắp xếp
        public string? SortBy { get; set; } // "name", "createdAt", "updatedAt"
        public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
    }
}
