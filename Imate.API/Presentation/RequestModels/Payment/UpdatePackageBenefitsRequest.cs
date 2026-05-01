namespace Imate.API.Presentation.RequestModels.Payment
{
    public class UpdatePackageBenefitsRequest
    {
        /// <summary>Danh sách benefits mới (mỗi phần tử là 1 dòng mô tả tính năng)</summary>
        public List<string> Benefits { get; set; } = new();
    }
}
