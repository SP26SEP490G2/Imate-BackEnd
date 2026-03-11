using Imate.API.Models.Enums;

namespace Imate.API.Presentation.RequestModels.QuestionBank
{
    public class ContributeQuestionRequestModel
    {
        public int CompanyId { get; set; }
        public int PositionId { get; set; }
        public Level Level { get; set; }
        public IEnumerable<int> SkillIds { get; set; }
        public DateOnly InterviewDate { get; set; }
        public int CategoryId { get; set; }
        public string QuestionContent { get; set; }
        public string? UserAnswer { get; set; }

        
    }
}
