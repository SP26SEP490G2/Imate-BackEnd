using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Payment
{
    public class UpdatePackagePriceRequest
    {
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0.")]
        public decimal Price { get; set; }
    }
}
