namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class SystemStatisticsResponse
    {
        public int TotalDeposit { get; set; } // Tổng tiền nạp vào (MoneyDeposit Success)
        public int TotalWithdrawal { get; set; } // Tổng tiền rút ra (MoneyWithdrawal Success)
        public int NetProfit { get; set; } // Lãi ròng = TotalDeposit - TotalWithdrawal
    }
}

