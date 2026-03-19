namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class CancelPreviewResponse
    {
        public string PackageToCancel { get; set; }
        public int RemainingDays { get; set; }
        public decimal RefundAmount { get; set; }
    }
}
