using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Mentors
{
    public class UpdateMentorPriceRequest
    {
        [Required(ErrorMessage = "Giá mỗi buổi không được để trống.")]
        [Range(0, 100000000, ErrorMessage = "Giá mỗi buổi phải từ 0 đến 100,000,000 imCoin.")]
        public int PricePerSession { get; set; }
    }
}
