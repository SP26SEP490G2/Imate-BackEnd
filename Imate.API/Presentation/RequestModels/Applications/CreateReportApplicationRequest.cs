using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Applications
{
    public class CreateReportApplicationRequest
    {
        [Required]
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } // Mô tả lỗi / Lý do
        [Required]
        public int BookingId { get; set; }

        public List<IFormFile>? EvidenceFiles { get; set; }
    }
}
