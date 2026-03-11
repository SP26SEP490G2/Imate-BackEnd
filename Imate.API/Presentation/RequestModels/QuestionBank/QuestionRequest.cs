using Imate.API.Business.Helper;
using Imate.API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.QuestionBank
{
    public class QuestionRequest
    {
        public class GetQuestionBankList
        {
            public string? SearchTerm { get; set; }
            public int? CategoryId { get; set; }
            public string? Difficulty { get; set; }
            public string? SortBy { get; set; } = "newest";
            public int PageNumber { get; set; } = 1;
            public int PageSize { get; set; } = 5;
        }
        public class GetSystemQuestionParams : QueryParameters
        {
            // Filter theo ID (cho phép chọn nhiều)
            public int? SkillId { get; set; }
            public int? PositionId { get; set; }
            public int? CategoryId { get; set; }

            // Filter theo Enum
            public DifficultyLevel? Difficulty { get; set; }

            // Filter theo trạng thái
            public bool? IsActive { get; set; }

            // Sắp xếp
            public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
            public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
        }
        public class GetContributedQuestionParams : QueryParameters
        {
            // Filter theo ID (cho phép chọn nhiều)
            public int? SkillId { get; set; }
            public int? PositionId { get; set; }
            public int? CategoryId { get; set; }
            public int? CompanyId { get; set; }

            // Filter theo Enum
            public Level? Level { get; set; }

            // Filter theo trạng thái
            public bool? IsActive { get; set; }

            // Sắp xếp
            public string? SortBy { get; set; } // "content", "createdAt", "updatedAt"
            public string? SortOrder { get; set; } = "asc"; // "asc" hoặc "desc"
        }
        public class CreateSystemQuestionForStaff
        {
            [Required(ErrorMessage = "Nội dung câu hỏi không được để trống.")]
            [StringLength(500, ErrorMessage = "Nội dung câu hỏi tối đa 500 ký tự.")]
            public string Content { get; set; }
            [Required(ErrorMessage = "Độ khó là bắt buộc.")]
            public DifficultyLevel Difficulty { get; set; }
            [Required(ErrorMessage = "Câu trả lời mẫu không được để trống.")]
            [StringLength(2000, ErrorMessage = "Câu trả lời mẫu tối đa 2000 ký tự.")]
            public string SampleAnswer { get; set; }
            [MinLength(1, ErrorMessage = "Phải có ít nhất một danh mục.")]
            public List<int> CategoryIds { get; set; }
            [MinLength(1, ErrorMessage = "Phải có ít nhất một kỹ năng.")]
            public List<int> SkillIds { get; set; }
            [MinLength(1, ErrorMessage = "Phải có ít nhất một vị trí.")]
            public List<int> PositionIds { get; set; }
            [Required(ErrorMessage = "Phải có người tạo.")]
            public int CreatorId { get; set; }
        }
        
    }
}
