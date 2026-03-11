using Imate.API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.QuestionBank
{
    public class UpdateSystemQuestionForStaffRequest
    {

        [Required(ErrorMessage = "Nội dung câu hỏi không được để trống.")]
        [StringLength(500, ErrorMessage = "Nội dung câu hỏi tối đa 500 ký tự.")]
        public string Content { get; set; }

        [Required(ErrorMessage = "Độ khó là bắt buộc.")]
        public DifficultyLevel Difficulty { get; set; }

        [Required(ErrorMessage = "Câu trả lời mẫu không được để trống.")]
        [StringLength(2000, ErrorMessage = "Câu trả lời mẫu tối đa 2000 ký tự.")]
        public string SampleAnswer { get; set; }
        public bool IsActive { get; set; }

        [MinLength(1, ErrorMessage = "Phải có ít nhất một lĩnh vực.")]
        public List<int> CategoryIds { get; set; }

        [MinLength(1, ErrorMessage = "Phải có ít nhất một kỹ năng.")]
        public List<int> SkillIds { get; set; }

        [MinLength(1, ErrorMessage = "Phải có ít nhất một vị trí.")]
        public List<int> PositionIds { get; set; }
    }
}
