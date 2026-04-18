using Imate.API.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.QuestionBank
{
    public class ContributeQuestionRequestModel
    {
        public int CompanyId { get; set; }
        public List<int> CategoryIds { get; set; }
        [MinLength(1, ErrorMessage = "Phải có ít nhất một kỹ năng.")]
        public List<int> SkillIds { get; set; }
        [MinLength(1, ErrorMessage = "Phải có ít nhất một vị trí.")]
        public List<int> PositionIds { get; set; }
        [Required(ErrorMessage = "Độ khó là bắt buộc.")]
        public DifficultyLevel Difficulty { get; set; }
        [Required(ErrorMessage = "Cấp độ là bắt buộc.")]
        public Level Level { get; set; }
        [Required(ErrorMessage = "Ngày phỏng vấn không được để trống")]
        public DateOnly InterviewDate { get; set; }
        [Required(ErrorMessage = "Nội dung câu hỏi không được để trống.")]
        public string Content { get; set; }
        [Required(ErrorMessage = "Độ khó là bắt buộc.")]
        public string? UserAnswer { get; set; }
    }
}
