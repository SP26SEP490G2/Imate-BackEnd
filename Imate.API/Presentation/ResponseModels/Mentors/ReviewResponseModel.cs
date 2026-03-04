namespace Imate.API.Presentation.ResponseModels.Mentors
{
    public class ReviewResponseModel
    {
        public int Score { get; set; }
        public string Text { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public string ReviewerFullName { get; set; }
        public string ReviewerAvatarUrl { get; set; }
    }
}
