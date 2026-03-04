namespace Imate.API.Presentation.ResponseModels.Mentors
{
    public class CandidateRatingsResponseModel
    {
        public int TotalRatingCount { get; set; }
        public decimal? AverageRating { get; set; }
        public List<RatingDetailModel> Ratings { get; set; } = new List<RatingDetailModel>();
    }

    public class RatingDetailModel
    {
        public int BookingId { get; set; }
        public string CandidateAvatar { get; set; }
        public string CandidateName { get; set; }
        public string ReviewText { get; set; }
        public int RatingScore { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
