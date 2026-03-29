using Imate.API.Models.Entities;

namespace Imate.API.DataAccess.Interfaces.Comunity
{
    public interface ICommentRepository
    {
        Task AddCommentAsync(Comment comment);
        Task<Comment?> GetCommentByIdAsync(int commentId);
        Task SaveChangesAsync();

        Task DeleteCommentAsync(Comment comment);
        Task<Comment?> GetCommentWithDetailsByIdAsync(int commentId);
    }
}
