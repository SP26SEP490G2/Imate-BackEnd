namespace Imate.API.Presentation.ResponseModels.Classification
{
    public class PaginatedCompanyResponseModel
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public long TotalCount { get; set; }
        public List<CompanyListItemResponseModel> Items { get; set; } = new List<CompanyListItemResponseModel>();
    }
    public class CompanyListItemResponseModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
