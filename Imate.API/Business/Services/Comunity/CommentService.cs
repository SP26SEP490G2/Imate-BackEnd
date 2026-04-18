using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Interfaces.Comunity;
using Imate.API.DataAccess;
using Imate.API.DataAccess.Interfaces.Comunity;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Comunity;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.AI.Module.Core.Interfaces;
using Imate.API.Business.Exceptions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Imate.API.Business.Services.Comunity
{
    public class CommentService : ICommentService
    {
        private readonly ICommentRepository _commentRepository;
        private readonly IVoteRepository _voteRepository;
        private readonly ImateDbContext _context;
        private readonly IGeminiService _geminiService;
        private readonly IMemoryCache _memoryCache;
        private readonly int _maxCommentsPerWindow;
        private readonly TimeSpan _rateLimitWindow;
        private readonly HashSet<string> _bannedSingleWords;
        private readonly List<string> _bannedPhrases;

        private static readonly char[] WordSeparators =
        {
            ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '-', '_',
            '(', ')', '[', ']', '{', '}', '"', '\'', '/', '\\', '|', '@',
            '#', '$', '%', '^', '&', '*', '+', '=', '<', '>'
        };

        public CommentService(
            ICommentRepository commentRepository, 
            IVoteRepository voteRepository,
            ImateDbContext context,
            IGeminiService geminiService,
            IMemoryCache memoryCache,
            IConfiguration configuration)
        {
            _commentRepository = commentRepository;
            _voteRepository = voteRepository;
            _context = context;
            _geminiService = geminiService;
            _memoryCache = memoryCache;

            var moderationConfig = configuration.GetSection("CommentModeration");
            _maxCommentsPerWindow = moderationConfig.GetValue<int?>("MaxCommentsPerWindow") ?? 5;
            var windowSeconds = moderationConfig.GetValue<int?>("RateLimitWindowSeconds") ?? 60;
            _rateLimitWindow = TimeSpan.FromSeconds(windowSeconds);

            var configuredWords = moderationConfig.GetSection("BannedWords").Get<string[]>() ?? Array.Empty<string>();
            if (configuredWords.Length == 0)
            {
                configuredWords = new[] { "dm", "vcl", "fuck", "shit" };
            }

            _bannedSingleWords = configuredWords
                .Where(word => !string.IsNullOrWhiteSpace(word) && !word.Contains(' '))
                .Select(word => word.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            _bannedPhrases = configuredWords
                .Where(word => !string.IsNullOrWhiteSpace(word) && word.Contains(' '))
                .Select(word => word.Trim())
                .ToList();
        }

        public async Task<int> CreateCommentAsync(int userId, CreateCommentRequestModel request)
        {
            var now = DateTime.UtcNow;

            EnforceCreateCommentRateLimit(userId, now);

            if (ContainsBannedWords(request.Content))
            {
                throw new BadRequestException("Bình luận chứa từ ngữ bị cấm. Vui lòng điều chỉnh nội dung.");
            }

            // Kiểm duyệt comment trước khi tạo
            //try
            //{
            //    var moderationResult = await _geminiService.ModerateCommentAsync(request.Content);

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
            //    // Nếu có lỗi khi kiểm duyệt (ví dụ: Gemini API lỗi), vẫn cho phép tạo comment
            //    // để tránh block người dùng khi service kiểm duyệt gặp sự cố
            //    // Có thể log lỗi ở đây để theo dõi
            //}

            var newComment = new Comment
            {
                UserId = userId,
                QuestionId = request.QuestionId,
                Content = request.Content,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _commentRepository.AddCommentAsync(newComment);

            return newComment.Id;
        }

        private void EnforceCreateCommentRateLimit(int userId, DateTime now)
        {
            var cacheKey = $"comment-create-rate-limit:{userId}";

            var commentTimestamps = _memoryCache.GetOrCreate(cacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = _rateLimitWindow;
                return new List<DateTime>();
            }) ?? new List<DateTime>();

            lock (commentTimestamps)
            {
                commentTimestamps.RemoveAll(timestamp => now - timestamp >= _rateLimitWindow);

                if (commentTimestamps.Count >= _maxCommentsPerWindow)
                {
                    throw new BadRequestException($"Bạn đang thao tác quá nhanh. Vui lòng thử lại sau {(int)_rateLimitWindow.TotalSeconds} giây.");
                }

                commentTimestamps.Add(now);
            }

            _memoryCache.Set(cacheKey, commentTimestamps, _rateLimitWindow);
        }

        private bool ContainsBannedWords(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return false;
            }

            if (_bannedPhrases.Any(phrase => content.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            var words = content
                .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return words.Any(word => _bannedSingleWords.Contains(word));
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

            var now = DateTime.UtcNow;

            EnforceCreateCommentRateLimit(userId, now);

            if (ContainsBannedWords(request.Content))
            {
                throw new BadRequestException("Bình luận chứa từ ngữ bị cấm. Vui lòng điều chỉnh nội dung.");
            }

            // Kiểm duyệt comment trước khi tạo
            //try
            //{
            //    var moderationResult = await _geminiService.ModerateCommentAsync(request.Content);

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
