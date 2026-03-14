using System.ComponentModel.DataAnnotations;

namespace Imate.API.Presentation.RequestModels.Comunity
{
    public class VoteCommentRequestModel
    {
        [Required]
        public bool IsUpvote { get; set; }
    }
}
