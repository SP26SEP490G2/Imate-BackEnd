    namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class UpgradePreviewResponse
    {
        // Gói mới
        public string NewPackageName { get; set; }
        public decimal NewPackagePrice { get; set; } // Giá gốc

        // Gói cũ (nếu có)
        public bool HasActiveSubscription { get; set; }
        public string OldPackageName { get; set; }
        public decimal RemainingValue { get; set; } // Số tiền còn lại của gói cũ

        // Kết quả
        public decimal AmountToCharge { get; set; } // Số tiền thực tế sẽ trừ
        public bool IsEligible { get; set; } // Có đủ đk nâng cấp không (không phải hạ cấp)
        public string Message { get; set; } // Thông báo (VD: "Không thể hạ cấp")
    }
}
