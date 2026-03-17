using Imate.API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Applications
{
    public class CreateTechnicalApplicationRequest
    {
        [Required]    
        public string? Title { get; set; }

        [Required]
        public string Content { get; set; } // Mô tả lỗi / Lý do

        public List<IFormFile>? EvidenceFiles { get; set; }
    }
}
