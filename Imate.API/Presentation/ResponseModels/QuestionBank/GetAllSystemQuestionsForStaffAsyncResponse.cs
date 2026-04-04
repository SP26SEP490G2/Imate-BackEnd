using Imate.API.Models.Enums;

namespace Imate.API.Presentation.ResponseModels.QuestionBank
{
    public class GetAllSystemQuestionsForStaffAsyncResponse
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public DifficultyLevel? Difficulty { get; set; }
        public bool IsFromSystem { get; set; }
        public bool IsActive { get; set; }
        public int CreatorId { get; set; }
        public string CreatorName { get; set; }
        public string? SampleAnswer { get; set; }
        public List<CommentDto> Comments { get; set; }
        public List<string> CategoriesName { get; set; }
        public List<string> SkillsName { get; set; }
        public List<string> PositionsName { get; set; }
       
    }  
}
