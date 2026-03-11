namespace Imate.API.Presentation.ResponseModels.QuestionBank
{
    public class ContributedQuestionDetailsResponseModel
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

        // Contributed Detail info
        public ContributedDetailDto ContributedDetail { get; set; }

        // Related data
        public List<string> Categories { get; set; }
        public List<string> Skills { get; set; }
        public List<string> Positions { get; set; }

        // Community engagement
        public List<CommentDto> Comments { get; set; }
        public int TotalComments { get; set; }

        public bool IsSaved { get; set; }
    }

    public class ContributedDetailDto
    {
        public int Id { get; set; }
        public DateOnly InterviewDate { get; set; }
        public string Level { get; set; }
        public string Company { get; set; }

        public string CompanyURL { get; set; }  
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserAvatarUrl { get; set; }
        public string UserRole { get; set; }
        public string Content { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
        public int UpvoteCount { get; set; }
        public int DownvoteCount { get; set; }
        public int TotalVotes { get; set; }
        public bool? CurrentUserVoteIsUpvote { get; set; }
    }
}
