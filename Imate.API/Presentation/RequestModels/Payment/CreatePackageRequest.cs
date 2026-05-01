namespace Imate.API.Presentation.RequestModels.Payment
{
    public class CreatePackageRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public List<string> Benefits { get; set; } = new();
        public bool IsRecommended { get; set; }
    }
}
