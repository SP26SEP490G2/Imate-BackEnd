namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class UpgradePreviewResponse
    {
        public string NewPackageName { get; set; }
        public decimal NewPackagePrice { get; set; }
        public bool IsEligible { get; set; }
        public string Message { get; set; }
    }
}