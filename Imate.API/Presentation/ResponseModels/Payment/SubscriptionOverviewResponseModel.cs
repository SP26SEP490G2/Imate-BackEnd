namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class SubscriptionOverviewResponseModel
    {
        // Đặt tên property khớp với ý nghĩa của card
        public OverviewCardData TotalPackagesSold { get; set; }
        public OverviewCardData TotalRevenue { get; set; }
        public OverviewCardData TopPackage { get; set; }
    }

    // 2. Model chung cho dữ liệu của MỘT card
    public class OverviewCardData
    {
        // Dùng 'object' để linh hoạt trả về số (45) hoặc chuỗi ("Gói gấp gáp")
        public object Value { get; set; }

        // Dùng List<decimal> hoặc List<int> tùy vào dữ liệu
        public List<long> Data { get; set; }

        // Model con cho phần "trend"
        public TrendData Trend { get; set; }
    }

    // 3. Model con cho "trend"
    public class TrendData
    {
        public decimal Percentage { get; set; }
        public bool IsPositive { get; set; }
    }
}
