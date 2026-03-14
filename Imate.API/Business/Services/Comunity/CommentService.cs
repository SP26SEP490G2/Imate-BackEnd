using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Interfaces.Comunity;
using Imate.API.DataAccess;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Comunity;
using Imate.API.DataAccess.ApplicationDbContext;

namespace Imate.API.Business.Services.Comunity
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IVoteRepository _voteRepository;
        private readonly ImateDbContext _context;

        public CommentService(
            ICommentRepository commentRepository, 
            IVoteRepository voteRepository,
            ImateDbContext context)
        {
            _commentRepository = commentRepository;
            _voteRepository = voteRepository;
            _context = context;
        }

        public async Task<int> CreateCommentAsync(int userId, CreateCommentRequestModel request)
        {
            // Kiểm duyệt comment trước khi tạo
            //try
            //{
            //    var moderationResult = await _openAIService.ModerateCommentAsync(request.Content);
                
            //    if (!moderationResult.IsSafe)
            //    {
            //        throw new BadRequestException("Nội dung không phù hợp. Vui lòng điều chỉnh nội dung.");
            //    }
            //}
            //catch (BadRequestException)
            //{
            //    throw; // Re-throw BadRequestException as is
            //}
            //catch (Exception ex)
            //{
            //    // Nếu có lỗi khi kiểm duyệt (ví dụ: OpenAI API lỗi), vẫn cho phép tạo comment
            //    // để tránh block người dùng khi service kiểm duyệt gặp sự cố
            //    // Có thể log lỗi ở đây để theo dõi
            //}

            var now = DateTime.UtcNow;

            var newComment = new Comment
            {
                UserId = userId,
                QuestionId = request.QuestionId,
                Content = request.Content,
                UpdatedAt = now
            };

            await _commentRepository.AddCommentAsync(newComment);

            return newComment.Id;
        }

        public async Task UpdateCommentAsync(int commentId, int userId, UpdateCommentRequestModel request)
        {
            var comment = await _commentRepository.GetCommentByIdAsync(commentId);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment với ID {commentId} không tồn tại.");
            }

            if (comment.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền chỉnh sửa bình luận này.");
            }

            // Kiểm duyệt comment trước khi cập nhật
            //try
            //{
            //    var moderationResult = await _openAIService.ModerateCommentAsync(request.Content);
                
            //    if (!moderationResult.IsSafe)
            //    {
            //        throw new BadRequestException("Nội dung không phù hợp. Vui lòng điều chỉnh nội dung.");
            //    }
            //}
            //catch (BadRequestException)
            //{
            //    throw; // Re-throw BadRequestException as is
            //}
            //catch (Exception ex)
            //{
            //    // Nếu có lỗi khi kiểm duyệt (ví dụ: OpenAI API lỗi), vẫn cho phép cập nhật comment
            //    // để tránh block người dùng khi service kiểm duyệt gặp sự cố
            //    // Có thể log lỗi ở đây để theo dõi
            //}

            comment.Content = request.Content;
            comment.UpdatedAt = DateTime.UtcNow; // Cập nhật thời gian sửa đổi

            await _commentRepository.SaveChangesAsync();
        }

        public async Task ToggleVoteAsync(int commentId, int userId, VoteCommentRequestModel request)
        {
            var targetIsUpvote = request.IsUpvote;
            var now = DateTime.UtcNow;

            var comment = await _voteRepository.GetCommentAuthorAsync(commentId);
            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment với ID {commentId} không tồn tại.");
            }

            if (comment.UserId == userId && targetIsUpvote == false)
            {
                throw new UnauthorizedAccessException("Bạn không thể Downvote bình luận của chính mình.");
            }

            var existingVote = await _voteRepository.GetVoteByKeysAsync(userId, commentId);

            if (existingVote == null)
            {
                var newVote = new Vote
                {
                    AccountId = userId,
                    CommentId = commentId,
                    IsUpvote = targetIsUpvote,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _voteRepository.AddVoteAsync(newVote);
            }
            else
            {
                if (existingVote.IsUpvote == targetIsUpvote)
                {
                    await _voteRepository.DeleteVoteAsync(existingVote);
                }
                else
                {
                    existingVote.IsUpvote = targetIsUpvote;
                    existingVote.UpdatedAt = now;
                    await _voteRepository.UpdateVoteAsync(existingVote);
                }
            }
        }

        public async Task DeleteCommentAsync(int commentId, int userId)
        {
            var comment = await _commentRepository.GetCommentByIdAsync(commentId);

            if (comment == null)
            {
                throw new KeyNotFoundException($"Comment với ID {commentId} không tồn tại.");
            }

            if (comment.UserId != userId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa bình luận này.");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var relatedVotes = await _voteRepository.GetVotesByCommentIdAsync(commentId);
                    if (relatedVotes.Count > 0)
                    {
                        _context.Set<Vote>().RemoveRange(relatedVotes);
                        await _voteRepository.SaveChangesAsync();
                    }

                    _context.Set<Comment>().Remove(comment);
                    await _commentRepository.SaveChangesAsync();

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw; 
                }
            }
        }
        //Test đến đây rồi
    }
}
