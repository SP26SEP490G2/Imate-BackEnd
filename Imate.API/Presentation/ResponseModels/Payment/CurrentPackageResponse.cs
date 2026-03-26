namespace Imate.API.Presentation.ResponseModels.Payment
{
    public class CurrentPackageResponse
    {
        public int PackageId { get; set; }
        public string PackageName { get; set; }
        public int Rank { get; set; }
        public decimal Price { get; set; }
    }
}
