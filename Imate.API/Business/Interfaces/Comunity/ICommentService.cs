using Imate.API.Presentation.RequestModels.Comunity;

namespace Imate.API.Business.Interfaces.Comunity
{
    public interface ICommentService
    {
        Task<int> CreateCommentAsync(int userId, CreateCommentRequestModel request);
        Task UpdateCommentAsync(int commentId, int userId, UpdateCommentRequestModel request);
        Task ToggleVoteAsync(int commentId, int userId, VoteCommentRequestModel request);
        Task DeleteCommentAsync(int commentId, int userId);
    }
}
