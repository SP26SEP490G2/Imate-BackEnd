namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class RevenueResponse
    {
        public int TotalRevenue { get; set; }
        public int TotalIncome { get; set; } // Tổng thu từ các loại transaction doanh thu
        public int TotalDeposit { get; set; } // Tổng PointDeposit (trừ đi)
        public RevenueBreakdown Breakdown { get; set; }
        public int Year { get; set; }
        public int? Month { get; set; }
        public string? TransactionType { get; set; }
    }

    public class RevenueBreakdown
    {
        public int PointBookingPayout { get; set; }
        public int PointSubscriptionFee { get; set; }
        public int PointPenalty { get; set; }
        public int PointInterviewFee { get; set; }
        public int PointDeposit { get; set; }
    }
}
