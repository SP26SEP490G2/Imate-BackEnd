namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class UserSubscriptionHistoryResponse
    {
        // Gói hiện tại (nếu có)
        public CurrentSubscriptionResponse? CurrentSubscription { get; set; }
        
        // Lịch sử mua gói
        public List<SubscriptionHistoryItem> History { get; set; } = new List<SubscriptionHistoryItem>();
        
        // Thông tin lượt phỏng vấn miễn phí (khi dùng gói thường hoặc không có gói)
        public FreeInterviewInfo? FreeInterviewInfo { get; set; }
    }
    
    public class FreeInterviewInfo
    {
        public int FreeUsedMock { get; set; }
        public int FreeLimit { get; set; }
        public int RemainingFree => FreeLimit - FreeUsedMock;
    }

    public class CurrentSubscriptionResponse
    {
        public int SubscriptionId { get; set; }
        public string PackageName { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public DateTime StartDateTime { get; set; } // Thời gian bắt đầu chính xác (từ CreatedAt)
        public DateTime? EndDateTime { get; set; } // Thời gian kết thúc chính xác (StartDateTime + DurationDays)
        public int InitialMockLimit { get; set; }
        public int MockInterviewUsed { get; set; }
        public int RemainingInterviews => InitialMockLimit - MockInterviewUsed;
        public bool IsActive { get; set; }
    }

    public class SubscriptionHistoryItem
    {
        public int SubscriptionId { get; set; }
        public string PackageName { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public DateTime StartDateTime { get; set; } // Thời gian bắt đầu chính xác (từ CreatedAt)
        public DateTime? EndDateTime { get; set; } // Thời gian kết thúc chính xác (StartDateTime + DurationDays)
        public decimal AmountPaid { get; set; } // Số tiền đã thanh toán
        public DateTime? TransactionDate { get; set; } // Ngày thanh toán
        public bool IsActive { get; set; }
    }
}

