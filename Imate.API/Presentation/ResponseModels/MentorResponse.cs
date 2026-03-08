namespace Imate.API.Presentation.ResponseModels
{
    public class MentorResponse
    {
        public class ListPreviewMentor
        {
            public int AccountId { get; set; }
            public string FullName { get; set; } = string.Empty;
            public string Position { get; set; } = string.Empty;
            public int Yoe { get; set; }
            public string Company { get; set; } = string.Empty;
            public decimal? AvgRatings { get; set; }
            public int? TotalRatingCount { get; set; }
        }
    }
}
