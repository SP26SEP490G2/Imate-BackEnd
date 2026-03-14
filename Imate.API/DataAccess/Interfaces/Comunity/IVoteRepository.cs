using Imate.API.Models.Entities;
namespace Imate.API.DataAccess.Interfaces.Comunity
{
    public interface IVoteRepository
    {
        Task<Vote?> GetVoteByKeysAsync(int accountId, int commentId);
        Task AddVoteAsync(Vote vote);
        Task UpdateVoteAsync(Vote vote);
        Task DeleteVoteAsync(Vote vote);
        Task<Comment?> GetCommentAuthorAsync(int commentId);
        Task SaveChangesAsync();

        Task<List<Vote>> GetVotesByCommentIdAsync(int commentId);
    }
}
