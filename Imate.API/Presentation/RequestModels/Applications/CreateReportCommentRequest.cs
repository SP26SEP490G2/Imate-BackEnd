using Imate.API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Applications
{
    public class CreateReportCommentRequest
    {
        [Required]
        public int CommentId { get; set; }

        [Required]
        public ReportReason Reason { get; set; }

        public string? AdditionalDetails { get; set; } // Mô tả thêm (optional)

        public List<IFormFile>? EvidenceFiles { get; set; } // Ảnh chứng cứ (optional)
    }
}

