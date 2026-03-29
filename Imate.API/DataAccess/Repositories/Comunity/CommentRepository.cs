using Microsoft.EntityFrameworkCore;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.DataAccess.Repositories.Comunity
{
    public class CommentRepository : ICommentRepository
    {
        private readonly ImateDbContext _context;

        public CommentRepository(ImateDbContext context)
        {
            _context = context;
        }

        public async Task AddCommentAsync(Comment comment)
        {
            
            _context.Set<Comment>().Add(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<Comment?> GetCommentByIdAsync(int commentId)
        {
            return await _context.Set<Comment>()
                                 .FirstOrDefaultAsync(c => c.Id == commentId);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCommentAsync(Comment comment)
        {
            _context.Set<Comment>().Remove(comment);
        }
        public async Task<Comment?> GetCommentWithDetailsByIdAsync(int commentId)
        {
            return await _context.Set<Comment>()
                .Include(c => c.User)
                .Include(c => c.Question)
                    .ThenInclude(q => q.Creator) 
                .FirstOrDefaultAsync(c => c.Id == commentId);
        }
    }
}
