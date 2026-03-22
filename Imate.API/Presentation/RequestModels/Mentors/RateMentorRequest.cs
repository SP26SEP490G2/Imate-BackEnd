using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Mentors
{
    public class RateMentorRequest
    {
        [Required(ErrorMessage = "RatingScore is required.")]
        [Range(1, 5, ErrorMessage = "RatingScore must be between 1 and 5.")]
        public int RatingScore { get; set; }

        [Required(ErrorMessage = "ReviewText is required.")]
        [MinLength(10, ErrorMessage = "ReviewText must be at least 10 characters.")]
        [MaxLength(1000, ErrorMessage = "ReviewText cannot exceed 1000 characters.")]
        public string ReviewText { get; set; } = null!;
    }
}
