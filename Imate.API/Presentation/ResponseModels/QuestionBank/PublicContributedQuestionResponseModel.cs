using Imate.API.Presentation.ResponseModels.QuestionBank;

namespace Imate.API.Presentation.ResponseModels.QuestionBank
{
    public class PublicContributedQuestionResponseModel
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsActive { get; set; }
        public string SampleAnswer { get; set; }    
        public DateTimeOffset CreatedAt { get; set; }

        // Creator info
        public int CreatorId { get; set; }
        public string CreatorName { get; set; }
        public string CreatorAvatarUrl { get; set; }
        public string CreatorRole { get; set; }
        public string Difficulty { get; set; }

        // Contributed Detail info
        public ContributedDetailDto ContributedDetail { get; set; }

        // Related data
        public List<CategoryDto> Categories { get; set; }
        public List<SkillDto> Skills { get; set; }
        public List<PositionDto> Positions { get; set; }

        public bool IsSaved { get; set; }
        public int CommentCount { get; set; }


    }

}
