using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Comunity
{
    public class CreateCommentRequestModel
    {
        [Required]
        public int QuestionId { get; set; }

        [Required]
        [StringLength(2000, MinimumLength = 1)]
        public string Content { get; set; } = null!;
    }
}
