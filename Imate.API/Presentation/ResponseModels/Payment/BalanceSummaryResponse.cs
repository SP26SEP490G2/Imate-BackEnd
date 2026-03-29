namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class BalanceSummaryResponse
    {
        public int CurrentBalance { get; set; } 
        public DateTimeOffset? LastUpdated { get; set; }
        public int TotalDeposit { get; set; }
        public int TotalWithdrawal { get; set; }
        
        // Thông tin cho Mentor
        public int? MaxBookingsCanReceive { get; set; } // Số lượt booking có thể nhận với số dư hiện tại
        public int? PricePerSession { get; set; } // Giá 1 session của mentor
        public int? CurrentEscrowBookings { get; set; } // Số booking đang escrow
        public int? RequiredBalanceForOneBooking { get; set; } // Số tiền đảm bảo cần cho 1 booking
        public decimal? GuaranteeDepositRate { get; set; } // Tỷ lệ tiền đảm bảo (%)
    }
}
