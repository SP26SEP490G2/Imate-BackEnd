using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories.Comunity
{
    public class VoteRepository : IVoteRepository
    {
        private readonly ImateDbContext _context;

        public VoteRepository(ImateDbContext context)
        {
            _context = context;
        }

        public Task<Vote?> GetVoteByKeysAsync(int accountId, int commentId)
        {
            return _context.Set<Vote>()
                           .FirstOrDefaultAsync(v => v.AccountId == accountId && v.CommentId == commentId);
        }

        public async Task AddVoteAsync(Vote vote)
        {
            _context.Set<Vote>().Add(vote);
            await _context.SaveChangesAsync();
        }

        public Task UpdateVoteAsync(Vote vote)
        {
            return _context.SaveChangesAsync();
        }

        public Task DeleteVoteAsync(Vote vote)
        {
            _context.Set<Vote>().Remove(vote);
            return _context.SaveChangesAsync();
        }

        public Task<Comment?> GetCommentAuthorAsync(int commentId)
        {
            return _context.Set<Comment>()
                           .Where(c => c.Id == commentId)
                           .FirstOrDefaultAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<List<Vote>> GetVotesByCommentIdAsync(int commentId)
        {
            return await _context.Set<Vote>()
                                 .Where(v => v.CommentId == commentId)
                                 .ToListAsync();
        }
    }
}
