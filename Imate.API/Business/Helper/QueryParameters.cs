namespace Imate.API.Business.Helper
{
    public abstract class QueryParameters
    {

        private const int MaxPageSize = 50;
        public int PageNumber { get; set; } = 1;

        private int _pageSize = 10;
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }    

        // Dùng cho tìm kiếm chung trên nhiều trường (ví dụ: tên, mô tả...)
        public string? SearchTerm { get; set; }

        // Dùng cho sắp xếp, ví dụ: "name_asc", "date_desc"
    }
}
