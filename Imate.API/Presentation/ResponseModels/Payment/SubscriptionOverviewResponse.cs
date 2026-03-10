namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class SubscriptionOverviewResponse
    {
        public int TotalSold { get; set; }
        public decimal TotalRevenue { get; set; }
        public string? FeaturedPackageName { get; set; }
        public List<MonthlySalesItem> MonthlySales { get; set; } = new();
    }

    public class MonthlySalesItem
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public Dictionary<string, int> PackageSales { get; set; } = new();
    }
}
