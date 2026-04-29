using ClosedXML.Excel;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.QuestionBank;
using Imate.API.Presentation.ResponseModels.Classification;
using Imate.API.Presentation.ResponseModels.QuestionBank;
using Imate.API.Presentation.SignalR.Events.QuestionBanks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;

namespace Imate.API.Business.Services.QuestionBank
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditLogService _auditLogService;
        private readonly ISystemConfigService _systemConfigService;
        private readonly ISystemNotificationService _systemNotificationService;
        private readonly ImateDbContext _context;
        private static readonly List<string> ExpectedHeaders = new List<string>
    {
        "Content",
        "Difficulty",
        "SampleAnswer",
        "CategoryNames",
        "SkillNames",
        "PositionNames"
    };
        public QuestionService(IUnitOfWork unitOfWork, ImateDbContext context, IAuditLogService auditLogService, ISystemConfigService systemConfigService, ISystemNotificationService systemNotificationService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _auditLogService = auditLogService;
            _systemConfigService = systemConfigService;
            _systemNotificationService = systemNotificationService;
        }

        public async Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync()
        {
            try
            {
                return await _unitOfWork.Questions.GetListHotQuestionsAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving hot questions.", ex);
            }
        }

        public async Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request)
        {
            try
            {
                var query = _unitOfWork.Questions.GetQuestionBankListAsync();
                // Apply search filter
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(q => q.Content.Contains(request.SearchTerm));
                }

                // Apply category filter
                if (request.CategoryId.HasValue)
                {
                    query = query.Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == request.CategoryId.Value));
                }

                // Apply difficulty filter
                if (!string.IsNullOrWhiteSpace(request.Difficulty))
                {
                    query = query.Where(q => q.Difficulty.ToString() == request.Difficulty);
                }

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "oldest" => query.OrderBy(q => q.CreatedAt),
                    "mostcommented" => query.OrderByDescending(q => q.Comments.Count),
                    _ => query.OrderByDescending(q => q.CreatedAt) // newest
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                var questions = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(q => new QuestionResponse.QuestionBankItem
                    {
                        Id = q.Id,
                        Title = q.Content.Length > 50 ? q.Content.Substring(0, 50) + "..." : q.Content,
                        Content = q.Content,
                        Categories = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                        Skills = q.QuestionSkills.Select(qs => qs.Skill.Name).ToList(),
                        Difficulty = q.Difficulty.HasValue ? q.Difficulty.ToString() : null,
                        CommentCount = q.Comments.Count,
                        CreatedBy = q.Creator.FullName,
                        CreatedAt = q.CreatedAt
                    })
                    .ToListAsync();
                return new QuestionResponse.QuestionBankList
                {
                    Questions = questions,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving question bank list.", ex);
            }
        }

        public async Task<PagedList<GetAllSystemQuestionsForStaffAsyncResponse>> GetAllSystemQuestionsForStaffAsync(GetSystemQuestionParams questionParams)
        {

            var query = _unitOfWork.Questions.GetAllSystemQuestionsForStaff();
            // 1. Filter theo SearchTerm (trên Content)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm));
            }

            // 2. Filter theo Trạng thái (IsActive)
            if (questionParams.IsActive.HasValue)
            {
                query = query.Where(q => q.IsActive == questionParams.IsActive.Value);
            }

            // 3. Filter theo Skill ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.SkillId.HasValue)
            {
                // Lấy những câu hỏi có chứa SkillId được cung cấp
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 4. Filter theo Position ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }
            // 5. Filter theo Category ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qp => qp.CategoryId == questionParams.CategoryId.Value));
            }
            // 5. Filter theo Difficulty (Level)
            if (questionParams.Difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == questionParams.Difficulty.Value);
            }





            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";

                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
    ? query.OrderByDescending(q => q.Content.Substring(0, 1).ToLower())
    : query.OrderBy(q => q.Content.Substring(0, 1).ToLower()),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),

                    _ => throw new NotFoundException($"Invalid SortBy value: {questionParams.SortBy}")
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu
                query = query.OrderByDescending(q => q.CreatedAt);
            }
            var response = query.Select(q => new GetAllSystemQuestionsForStaffAsyncResponse
            {
                Id = q.Id,
                Content = q.Content,
                Difficulty = q.Difficulty,
                IsFromSystem = q.IsFromSystem,
                IsActive = q.IsActive,
                CreatorId = q.CreatorId,
                CreatorName = q.Creator.FullName,
                SampleAnswer = q.SampleAnswer,
                CategoriesName = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                SkillsName = q.QuestionSkills.Select(qs => qs.Skill.Name).ToList(),
                PositionsName = q.QuestionPositions.Select(qp => qp.Position.Name).ToList()
            });
            return await PagedList<GetAllSystemQuestionsForStaffAsyncResponse>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);

        }
        public async Task<PagedList<GetAllContributedQuestionsForStaffAsyncResponse>> GetAllContributedQuestionsForStaffAsync(GetContributedQuestionParams questionParams)
        {
            var query = _unitOfWork.Questions.GetAllContributedForStaffQuestions();

            // Filter: Chỉ hiển thị các câu hỏi đã được approve (ApprovalStatus = Approved)
            // Loại bỏ các câu hỏi đang chờ duyệt (Pending) và các câu hỏi bị reject (Rejected)
            query = query.Where(q => q.ApprovalStatus == QuestionApprovalStatus.Approved);

            // 1. Filter theo SearchTerm (trên Content)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm));
            }

            // 2. Filter theo Trạng thái (IsActive) - Optional, có thể filter thêm nếu cần
            if (questionParams.IsActive.HasValue)
            {
                query = query.Where(q => q.IsActive == questionParams.IsActive.Value);
            }

            // 3. Filter theo Skill ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.SkillId.HasValue)
            {
                // Lấy những câu hỏi có chứa SkillId được cung cấp
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 4. Filter theo Position ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }
            // 5. Filter theo Category ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qp => qp.CategoryId == questionParams.CategoryId.Value));
            }
            // 5. Filter theo Company ID
            if (questionParams.CompanyId.HasValue)
            {
                query = query.Where(q => q.ContributedDetail.CompanyId == questionParams.CompanyId.Value);
            }

            // 6. Filter theo Difficulty (Level)
            if (questionParams.Level.HasValue)
            {
                query = query.Where(q => q.ContributedDetail.Level == questionParams.Level.Value);
            }


            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";

                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
    ? query.OrderByDescending(q => q.Content.Substring(0, 1).ToLower())
    : query.OrderBy(q => q.Content.Substring(0, 1).ToLower()),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu
                query = query.OrderByDescending(q => q.CreatedAt);
            }

            var response = query.Select(q => new GetAllContributedQuestionsForStaffAsyncResponse
            {
                Id = q.Id,
                Content = q.Content ?? string.Empty,
                Difficulty = q.Difficulty,
                IsFromSystem = q.IsFromSystem,
                IsActive = q.IsActive,
                CreatorId = q.CreatorId,
                CreatorName = q.Creator.FullName ?? string.Empty,
                SampleAnswer = q.SampleAnswer,
                ContributedDetailId = q.ContributedDetailId,
                Level = q.ContributedDetail.Level,
                CompanyName = q.ContributedDetail.Company.Name,
                CategoriesName = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                SkillsName = q.QuestionSkills.Select(qs => qs.Skill.Name ?? string.Empty).ToList(),
                PositionsName = q.QuestionPositions.Select(qp => qp.Position.Name ?? string.Empty).ToList()

            });

            return await PagedList<GetAllContributedQuestionsForStaffAsyncResponse>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);
        }

        public async Task<GetAllContributedQuestionsForStaffAsyncResponse> GetContributedQuestionByIdAsync(int questionId, int? accountId)
        {
            var a = await _unitOfWork.Questions.GetQuestionByIdAsync(questionId, false);
            if (a == null) throw new NotFoundException($"Không tìm được câu hỏi hệ thống");

            var newa = new GetAllContributedQuestionsForStaffAsyncResponse
            {
                Id = a.Id,
                Content = a.Content,
                Difficulty = a.Difficulty,
                SampleAnswer = a.SampleAnswer,
                IsFromSystem = a.IsFromSystem,
                IsActive = a.IsActive,
                CreatorId = a.CreatorId,
                CreatorName = a.Creator?.FullName ?? "Unknown",
                CategoriesName = a.QuestionCategories.Select(c => c.Category.Name).ToList(),
                SkillsName = a.QuestionSkills.Select(s => s.Skill.Name).ToList(),
                PositionsName = a.QuestionPositions.Select(p => p.Position.Name).ToList(),
                Level = a.ContributedDetail.Level,
                CompanyName = a.ContributedDetail.Company.Name,
                ContributedDetailId = a.ContributedDetailId,
                Comments = a.Comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    UserId = c.UserId,
                    UserName = c.User?.FullName ?? "Unknown",
                    UserAvatarUrl = c.User?.AvatarUrl ?? string.Empty,
                    UserRole = c.User?.AccountRoles?.FirstOrDefault()?.Role?.Name.ToString() ?? "Candidate",
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    UpvoteCount = c.Votes?.Count(v => v.IsUpvote) ?? 0,
                    DownvoteCount = c.Votes?.Count(v => !v.IsUpvote) ?? 0,
                    TotalVotes = c.Votes?.Count ?? 0,
                    CurrentUserVoteIsUpvote = accountId.HasValue ? c.Votes?.FirstOrDefault(v => v.AccountId == accountId.Value)?.IsUpvote : null
                }).ToList()

                // THAY THẾ .ToListAsync() bằng .FirstOrDefaultAsync() để chỉ lấy 1 đối tượng.


            };
            return newa;
        }
        public async Task<List<HiddenQuestion>> GetAllQuestionsHiddenAsync(AllQuestionParams questionParams)
        {
            var query = _unitOfWork.Questions.GetAllQuestions();

            // Lọc theo SkillId nếu có
            if (questionParams.SkillIds.HasValue)
            {
                query = query.Where(q => q.QuestionSkills
                    .Any(qs => qs.SkillId == questionParams.SkillIds.Value) && q.IsActive);
            }

            // Lọc theo PositionId nếu có
            if (questionParams.PositionIds.HasValue)
            {
                query = query.Where(q => q.QuestionPositions
                    .Any(qp => qp.PositionId == questionParams.PositionIds.Value) && q.IsActive);
            }

            // Lọc theo CategoryId nếu có
            if (questionParams.CategoryIds.HasValue)
            {
                query = query.Where(q => q.QuestionCategories
                    .Any(qc => qc.CategoryId == questionParams.CategoryIds.Value) && q.IsActive);
            }

            var response = await query
                .Select(q => new HiddenQuestion
                {
                    Id = q.Id,
                    Content = q.Content
                })
                .ToListAsync(); // 👈 Dùng ToListAsync thay vì ToList

            return response;
        }
        public async Task<PagedList<GetAllContributedQuestionsForStaffAsyncResponse>> GetAllPendingContributedQuestionForStaffAsync(PendingContributedParams questionParams)
        {
            // Filter: Chỉ lấy các câu hỏi đang chờ duyệt (ApprovalStatus = Pending)
            var query = _unitOfWork.Questions.GetAllContributedForStaffQuestions().Where(q => q.ApprovalStatus == QuestionApprovalStatus.Pending);
            // 1. Filter theo SearchTerm (trên Content)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm));
            }

            // 3. Filter theo Skill ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.SkillId.HasValue)
            {
                // Lấy những câu hỏi có chứa SkillId được cung cấp
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 4. Filter theo Position ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }
            // 5. Filter theo Category ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qp => qp.CategoryId == questionParams.CategoryId.Value));
            }
            // 5. Filter theo Company ID
            if (questionParams.CompanyId.HasValue)
            {
                query = query.Where(q => q.ContributedDetail.CompanyId == questionParams.CompanyId.Value);
            }

            // 6. Filter theo Difficulty (Level)
            if (questionParams.Level.HasValue)
            {
                query = query.Where(q => q.ContributedDetail.Level == questionParams.Level.Value);
            }


            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";

                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
    ? query.OrderByDescending(q => q.Content.Substring(0, 1).ToLower())
    : query.OrderBy(q => q.Content.Substring(0, 1).ToLower()),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu
                query = query.OrderByDescending(q => q.CreatedAt);
            }

            var response = query.Select(q => new GetAllContributedQuestionsForStaffAsyncResponse
            {
                Id = q.Id,
                Content = q.Content ?? string.Empty,
                Difficulty = q.Difficulty,
                IsFromSystem = q.IsFromSystem,
                IsActive = q.IsActive,
                CreatorId = q.CreatorId,
                CreatorName = q.Creator.FullName ?? string.Empty,
                SampleAnswer = q.SampleAnswer,
                Level = q.ContributedDetail.Level,
                CompanyName = q.ContributedDetail.Company.Name,
                CategoriesName = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                SkillsName = q.QuestionSkills.Select(qs => qs.Skill.Name ?? string.Empty).ToList(),
                PositionsName = q.QuestionPositions.Select(qp => qp.Position.Name ?? string.Empty).ToList()

            });

            return await PagedList<GetAllContributedQuestionsForStaffAsyncResponse>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);
        }

        public async Task<ContributedQuestionDetailsResponseModel> GetPublicContributedQuestionByIdAsync(int questionId, int? accountId)
        {
            var question = await _unitOfWork.Questions.GetQuestionByIdWithRelatedDataAsync(questionId);

            var savedQuestionIds = new HashSet<int>();

            if (accountId.HasValue)
            {
                var savedQuestions = await _unitOfWork.Questions.GetSavedQuestionIdsByAccountAsync(accountId.Value);
                savedQuestionIds = new HashSet<int>(savedQuestions);
            }

            if (question == null)
            {
                return null;
            }

            return MapToQuestionDetialsResponseModel(question, accountId, savedQuestionIds);
        }

        public async Task<IEnumerable<PublicContributedQuestionResponseModel>> GetAllPublicContributedQuestionAsync(string subscription, int? accountId)
        {
            IEnumerable<Question> questions; // (Giả sử Entity tên là Question)
            if (subscription == "Gói Thường" || subscription == null)
            {
                // 1. GÓI FREE: Gọi phương thức Repository mới (Lấy 10 câu / thể loại)
                questions = await _unitOfWork.Questions.GetLimitedContributedQuestionsWithRelatedDataAsync();
            }
            else
            {
                questions = await _unitOfWork.Questions.GetAllContributedQuestionsWithRelatedDataAsync();
            }


            var savedQuestionIds = new HashSet<int>();

            if (accountId.HasValue)
            {
                // ⭐ Giả định bạn đã sửa lại phương thức này trong Repository để dùng int (nếu QuestionId là int)
                // Nếu QuestionId là Guid, bạn cần dùng Guid và thay đổi kiểu dữ liệu của HashSet
                var savedQuestions = await _unitOfWork.Questions.GetSavedQuestionIdsByAccountAsync(accountId.Value);

                // Chuyển kết quả sang HashSet<int> (Giả định QuestionId là int)
                savedQuestionIds = new HashSet<int>((IEnumerable<int>)savedQuestions);
            }
            // 3. Ánh xạ, truyền HashSet vào hàm mapping
            return questions.Select(q => MapToQuestionListResponseModel(q, savedQuestionIds)).ToList();
        }

        public async Task<IEnumerable<PublicSystemQuestionResponseModel>> GetPublicSystemQuestionBanksAsync(string subscription, int? accountId = null)
        {
            IEnumerable<Question> questions = await _unitOfWork.Questions.GetPublicSystemQuestionBanksAsync();

            var savedQuestionIds = new HashSet<int>();

            if (accountId.HasValue)
            {
                // ⭐ Giả sử phương thức này trả về IEnumerable<int> (là các QuestionId)
                var savedQuestions = await _unitOfWork.Questions.GetSavedQuestionIdsByAccountAsync(accountId.Value);

                // Chuyển kết quả sang HashSet để tra cứu hiệu quả hơn
                savedQuestionIds = new HashSet<int>(savedQuestions);
            }

            return questions.Select(q => new PublicSystemQuestionResponseModel
            {
                Id = q.Id,
                Content = q.Content,
                Difficulty = q.Difficulty.ToString(),
                SampleAnswer = q.SampleAnswer,
                CreatorName = q.Creator?.FullName,
                CreatedAt = q.CreatedAt,
                Categories = q.QuestionCategories?.Select(qc => new CategoryDto
                {
                    Id = qc.CategoryId,
                    Name = qc.Category.Name
                }).ToList(),
                Skills = q.QuestionSkills?.Select(qs => new SkillDto
                {
                    Id = qs.SkillId,
                    Name = qs.Skill.Name
                }).ToList(),
                Positions = q.QuestionPositions?.Select(qp => new PositionDto
                {
                    Id = qp.PositionId,
                    Name = qp.Position.Name
                }).ToList(),
                IsSaved = accountId.HasValue && savedQuestionIds.Contains(q.Id),
                CommentCount = q.Comments != null ? q.Comments.Count : 0
            }).ToList();
        }

        public async Task<PagedList<PublicSystemQuestionResponseModel>> GetPublicSystemQuestionBanksWithPaginationAsync(string subscription, int? accountId, GetPublicSystemQuestionParams questionParams)
        {
            // Lấy query từ repository - chỉ lấy active questions
            var query = _unitOfWork.Questions.GetAllQuestions()
                .Where(q => q.IsFromSystem && q.IsActive)
                .Include(q => q.Creator)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .AsNoTracking();

            // 1. Filter theo SearchTerm (trên Content và SampleAnswer)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm) ||
                    (q.SampleAnswer != null && q.SampleAnswer.ToLower().Contains(searchTerm)));
            }

            // 2. Filter theo Skill ID
            if (questionParams.SkillId.HasValue)
            {
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 3. Filter theo Position ID
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }

            // 4. Filter theo Category ID
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == questionParams.CategoryId.Value));
            }

            // 5. Filter theo Difficulty
            if (questionParams.Difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == questionParams.Difficulty.Value);
            }

            // 6. Sorting
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";
                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
                        ? query.OrderByDescending(q => q.Content)
                        : query.OrderBy(q => q.Content),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),
                    "popular" => isDescending
                        ? query.OrderByDescending(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt),
                    _ => query.OrderByDescending(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt);
            }

            // Get saved question IDs
            var savedQuestionIds = new HashSet<int>();
            if (accountId.HasValue)
            {
                var savedQuestions = await _unitOfWork.Questions.GetSavedQuestionIdsByAccountAsync(accountId.Value);
                savedQuestionIds = new HashSet<int>(savedQuestions);
            }

            // Map to response model
            var response = query.Select(q => new PublicSystemQuestionResponseModel
            {
                Id = q.Id,
                Content = q.Content,
                Difficulty = q.Difficulty.ToString(),
                SampleAnswer = q.SampleAnswer,
                CreatorName = q.Creator != null ? q.Creator.FullName : null,
                CreatedAt = q.CreatedAt,
                Categories = q.QuestionCategories.Select(qc => new CategoryDto
                {
                    Id = qc.CategoryId,
                    Name = qc.Category.Name
                }).ToList(),
                Skills = q.QuestionSkills.Select(qs => new SkillDto
                {
                    Id = qs.SkillId,
                    Name = qs.Skill.Name
                }).ToList(),
                Positions = q.QuestionPositions.Select(qp => new PositionDto
                {
                    Id = qp.PositionId,
                    Name = qp.Position.Name
                }).ToList(),
                IsSaved = accountId.HasValue && savedQuestionIds.Contains(q.Id),
                CommentCount = q.Comments.Count
            });

            return await PagedList<PublicSystemQuestionResponseModel>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);
        }

        public async Task<PagedList<PublicContributedQuestionResponseModel>> GetPublicContributedQuestionBanksWithPaginationAsync(string subscription, int? accountId, GetPublicContributedQuestionParams questionParams)
        {
            // Lấy query từ repository - chỉ lấy active và approved questions
            var query = _unitOfWork.Questions.GetAllQuestions()
                .Where(q => !q.IsFromSystem && q.IsActive && q.ApprovalStatus == QuestionApprovalStatus.Approved)
                .Include(q => q.Creator)
                    .ThenInclude(c => c.AccountRoles)
                        .ThenInclude(ar => ar.Role)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .Include(q => q.ContributedDetail)
                    .ThenInclude(cd => cd.Company)
                .AsNoTracking();

            // 1. Filter theo SearchTerm (trên Content và SampleAnswer)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm) ||
                    (q.SampleAnswer != null && q.SampleAnswer.ToLower().Contains(searchTerm)));
            }

            // 2. Filter theo Skill ID
            if (questionParams.SkillId.HasValue)
            {
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 3. Filter theo Position ID
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }

            // 4. Filter theo Category ID
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == questionParams.CategoryId.Value));
            }

            // 5. Filter theo Company ID
            if (questionParams.CompanyId.HasValue)
            {
                query = query.Where(q => q.ContributedDetail != null && q.ContributedDetail.CompanyId == questionParams.CompanyId.Value);
            }

            // 6. Filter theo Company Name (string search)
            if (!string.IsNullOrWhiteSpace(questionParams.CompanyName))
            {
                var companyName = questionParams.CompanyName.ToLower().Trim();
                query = query.Where(q => q.ContributedDetail != null &&
                    q.ContributedDetail.Company != null &&
                    q.ContributedDetail.Company.Name.ToLower().Contains(companyName));
            }

            // 7. Filter theo Level
            if (questionParams.Level.HasValue)
            {
                query = query.Where(q => q.ContributedDetail != null && q.ContributedDetail.Level == questionParams.Level.Value);
            }
            if (questionParams.Difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == questionParams.Difficulty.Value);
            }


            // 8. Sorting
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";
                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
                        ? query.OrderByDescending(q => q.Content)
                        : query.OrderBy(q => q.Content),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),
                    "popular" => isDescending
                        ? query.OrderByDescending(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt),
                    _ => query.OrderByDescending(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt)
                };
            }
            else
            {
                query = query.OrderByDescending(q => q.Comments.Count).ThenByDescending(q => q.CreatedAt);
            }

            // Get saved question IDs
            var savedQuestionIds = new HashSet<int>();
            if (accountId.HasValue)
            {
                var savedQuestions = await _unitOfWork.Questions.GetSavedQuestionIdsByAccountAsync(accountId.Value);
                savedQuestionIds = new HashSet<int>(savedQuestions);
            }

            // Map to response model
            var response = query.Select(q => new PublicContributedQuestionResponseModel
            {
                Id = q.Id,
                Content = q.Content,
                IsActive = q.IsActive,
                SampleAnswer = q.SampleAnswer ?? string.Empty,
                CreatedAt = q.CreatedAt,
                CreatorId = q.CreatorId,
                CreatorName = q.Creator != null ? q.Creator.FullName : "Unknown",
                CreatorAvatarUrl = q.Creator != null ? q.Creator.AvatarUrl ?? string.Empty : string.Empty,
                CreatorRole = q.Creator != null && q.Creator.AccountRoles != null && q.Creator.AccountRoles.Any()
                    ? q.Creator.AccountRoles.First().Role.Name.ToString()
                    : "Member",
                Categories = q.QuestionCategories.Select(qc => new CategoryDto
                {
                    Id = qc.CategoryId,
                    Name = qc.Category.Name
                }).ToList(),
                Skills = q.QuestionSkills.Select(qs => new SkillDto
                {
                    Id = qs.SkillId,
                    Name = qs.Skill.Name ?? string.Empty
                }).ToList(),
                Positions = q.QuestionPositions.Select(qp => new PositionDto
                {
                    Id = qp.PositionId,
                    Name = qp.Position.Name ?? string.Empty
                }).ToList(),
                ContributedDetail = q.ContributedDetail != null ? new ContributedDetailDto
                {
                    Id = q.ContributedDetail.Id,
                    InterviewDate = q.ContributedDetail.InterviewDate,
                    Level = q.ContributedDetail.Level.ToString(),
                    Company = q.ContributedDetail.Company != null ? q.ContributedDetail.Company.Name : string.Empty,
                    CompanyURL = q.ContributedDetail.Company != null ? q.ContributedDetail.Company.ImageUrl : string.Empty,
                } : null,
                Difficulty = q.Difficulty.ToString(),
                IsSaved = accountId.HasValue && savedQuestionIds.Contains(q.Id),
                CommentCount = q.Comments.Count
            });

            return await PagedList<PublicContributedQuestionResponseModel>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);
        }

        private PublicContributedQuestionResponseModel MapToQuestionListResponseModel(Question question, HashSet<int> savedQuestionIds)
        {
            var response = new PublicContributedQuestionResponseModel
            {
                Id = question.Id,
                Content = question.Content,
                IsActive = question.IsActive,
                SampleAnswer = question.SampleAnswer ?? string.Empty,
                CreatedAt = question.CreatedAt,
                CreatorId = question.CreatorId,
                CreatorName = question.Creator?.FullName ?? "Unknown",
                CreatorAvatarUrl = question.Creator?.AvatarUrl ?? string.Empty,
                CreatorRole = (question.Creator?.AccountRoles?.FirstOrDefault()?.Role?.Name)?.ToString() ?? "Member",

                Categories = question.QuestionCategories?.Select(qc => new CategoryDto
                {
                    Id = qc.CategoryId,
                    Name = qc.Category.Name
                }).ToList(),
                Skills = question.QuestionSkills?.Select(qs => new SkillDto
                {
                    Id = qs.SkillId,
                    Name = qs.Skill.Name
                }).ToList(),
                Positions = question.QuestionPositions?.Select(qp => new PositionDto
                {
                    Id = qp.PositionId,
                    Name = qp.Position.Name
                }).ToList(),

                // sua ơ đây
                ContributedDetail = question.ContributedDetail != null ? new ContributedDetailDto
                {
                    Id = question.ContributedDetail.Id,
                    InterviewDate = question.ContributedDetail.InterviewDate,
                    Level = question.ContributedDetail.Level.ToString(),
                    Company = question.ContributedDetail.Company.Name,
                    CompanyURL = question.ContributedDetail.Company.ImageUrl,
                } : null,
                IsSaved = savedQuestionIds.Count > 0 && savedQuestionIds.Contains(question.Id),
                CommentCount = question.Comments != null ? question.Comments.Count : 0
            };

            return response;
        }

        private ContributedQuestionDetailsResponseModel MapToQuestionDetialsResponseModel(Question question, int? accountId, HashSet<int> savedQuestionIds)
        {
            var response = new ContributedQuestionDetailsResponseModel
            {
                Id = question.Id,
                Content = question.Content,
                IsActive = question.IsActive,
                SampleAnswer = question.SampleAnswer ?? string.Empty,
                CreatedAt = question.CreatedAt,
                CreatorId = question.CreatorId,
                CreatorName = question.Creator?.FullName ?? "Unknown",
                CreatorAvatarUrl = question.Creator?.AvatarUrl ?? string.Empty,
                CreatorRole = (question.Creator?.AccountRoles?.FirstOrDefault()?.Role?.Name)?.ToString() ?? "Member",

                Categories = question.QuestionCategories?
                    .Select(qc => qc.Category?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList() ?? new List<string>(),

                Skills = question.QuestionSkills?
                    .Select(qs => qs.Skill?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList() ?? new List<string>(),

                Positions = question.QuestionPositions?
                    .Select(qp => qp.Position?.Name)
                    .Where(name => !string.IsNullOrEmpty(name))
                    .ToList() ?? new List<string>(),

                ContributedDetail = question.ContributedDetail != null ? new ContributedDetailDto
                {
                    Id = question.ContributedDetail.Id,
                    InterviewDate = question.ContributedDetail.InterviewDate,
                    Level = question.ContributedDetail.Level.ToString(),
                    Company = question.ContributedDetail.Company.Name,
                    CompanyURL = question.ContributedDetail.Company.ImageUrl,
                } : null,

                Comments = question.Comments?
                    .Select(c =>
                    {
                        bool? currentUserVote = null;
                        if (accountId.HasValue)
                        {
                            var vote = c.Votes?.FirstOrDefault(v => v.AccountId == accountId.Value);
                            if (vote != null)
                            {
                                currentUserVote = vote.IsUpvote;
                            }
                        }
                        // Get user role from AccountRoles
                        var roleName = c.User?.AccountRoles?.FirstOrDefault()?.Role?.Name;
                        var userRole = roleName?.ToString() ?? "Member";
                        return new CommentDto
                        {
                            Id = c.Id,
                            UserId = c.UserId,
                            UserName = c.User?.FullName ?? "Unknown",
                            UserAvatarUrl = c.User?.AvatarUrl ?? string.Empty,
                            UserRole = userRole,
                            Content = c.Content,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt,
                            UpvoteCount = c.Votes?.Count(v => v.IsUpvote) ?? 0,
                            DownvoteCount = c.Votes?.Count(v => !v.IsUpvote) ?? 0,
                            TotalVotes = c.Votes?.Count ?? 0,
                            CurrentUserVoteIsUpvote = currentUserVote
                        };
                    })
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList() ?? new List<CommentDto>(),

                TotalComments = question.Comments?.Count ?? 0,
                IsSaved = accountId.HasValue && savedQuestionIds.Contains(question.Id)
            };

            return response;
        }

        //Candidate đóng góp câu hỏi
        public async Task<ContributionFormDataResponseModel> GetContributionFormDataAsync()
        {
            var companies = await _unitOfWork.Questions.GetCompaniesAsync();
            var categories = await _unitOfWork.Questions.GetCategoriesAsync();
            var positions = await _unitOfWork.Questions.GetPositionsWithSkillsAsync();

            return new ContributionFormDataResponseModel
            {
                Companies = companies.Select(c => new CompanyResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    ImageUrl = c.ImageUrl
                }),
                Categories = categories.Select(c => new Presentation.ResponseModels.QuestionBank.CategoryResponse
                {
                    Id = c.Id,
                    Name = c.Name
                }),
                Positions = positions.Select(p => new PositionWithSkillsResponse
                {
                    Id = p.Id,
                    Name = p.Name
                })
            };
        }

        public async Task CreateContributedQuestionAsync(ContributeQuestionRequestModel request, int creatorId)
        {
            var contributedDetail = new ContributedDetail
            {
                InterviewDate = request.InterviewDate,
                Level = request.Level,
                CompanyId = request.CompanyId
            };

            // Check trùng content
            var existingContents = await _unitOfWork.Questions.FindExistingContentsAsync(new List<string> { request.Content });
            if (existingContents.Any())
            {
                throw new BadRequestException("Nội dung câu hỏi đã tồn tại trong hệ thống.");
            }

            var question = new Question
            {
                Content = request.Content,
                SampleAnswer = request.UserAnswer,
                CreatorId = creatorId,
                Difficulty = request.Difficulty,
                IsFromSystem = false,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                ApprovalStatus = QuestionApprovalStatus.Pending, // Mặc định là Pending khi tạo mới
                ContributedDetail = contributedDetail,
                QuestionSkills = request.SkillIds.Select(skillId => new QuestionSkill { SkillId = skillId }).ToList()
            };
            var a = new List<int>();
            // 2. Kiểm tra tồn tại của Creator ID
            if (await _unitOfWork.Accounts.AreUsersExisted(creatorId) == false)
                throw new NotFoundException($"Creator ID {creatorId} không tồn tại.");

            // --- BẮT ĐẦU PHẦN ÁNH XẠ VÀ VALIDATION CHO CÁC MỐI QUAN HỆ ---

            // 3. Ánh xạ và Validate Category IDs
            if (request.CategoryIds?.Any() == true)
            {
                // 3a. Validate: Kiểm tra từng ID (Nên tối ưu thành 1 Query như đã thảo luận)
                a = await _unitOfWork.Categories.GetNonExistingCategoryIdsAsync(request.CategoryIds);
                if (a.Any())
                    throw new NotFoundException($"Category ID {string.Join(", ", a)} không tồn tại.");


                // 3b. ÁNH XẠ: Tạo Collection các QuestionCategory
                question.QuestionCategories = request.CategoryIds
                    .Select(cId => new QuestionCategory
                    {
                        CategoryId = cId,
                        Question = question // Liên kết ngược (nếu cần thiết cho EF Core)
                    })
                    .ToList();
            }

            // 4. Ánh xạ và Validate Skill IDs
            if (request.SkillIds?.Any() == true)
            {
                a = await _unitOfWork.Skills.GetNonExistingSkillIdsAsync(request.SkillIds);
                if (a.Any())
                    throw new NotFoundException($"Skill ID {string.Join(", ", a)} không tồn tại.");


                // 4b. ÁNH XẠ: Tạo Collection các QuestionSkill
                question.QuestionSkills = request.SkillIds
                    .Select(sId => new QuestionSkill
                    {
                        SkillId = sId,
                        Question = question
                    })
                    .ToList();
            }

            // 5. Ánh xạ và Validate Position IDs
            if (request.PositionIds?.Any() == true)
            {
                a = await _unitOfWork.Positions.GetNonExistingPositionIdsAsync(request.PositionIds);
                if (a.Any())
                    throw new NotFoundException($"Position ID {string.Join(", ", a)} không tồn tại.");


                // 5b. ÁNH XẠ: Tạo Collection các QuestionPosition
                question.QuestionPositions = request.PositionIds
                    .Select(pId => new QuestionPosition
                    {
                        PositionId = pId,
                        Question = question
                    })
                    .ToList();
            }

            // --- KẾT THÚC ÁNH XẠ ---
            await _unitOfWork.Questions.CreateContributedQuestionAsync(question);
        }

        public async Task<Question> CreateSystemQuestionForStaffAsync(CreateSystemQuestionForStaffRequest request, int creatorId)
        {
            // Check trùng content
            var existingContents = await _unitOfWork.Questions.FindExistingContentsAsync(new List<string> { request.Content });
            if (existingContents.Any())
            {
                throw new BadRequestException("Nội dung câu hỏi đã tồn tại trong hệ thống.");
            }

            // 1. Ánh xạ thuộc tính cơ bản
            var question = new Question
            {
                Content = request.Content,
                Difficulty = request.Difficulty,
                SampleAnswer = request.SampleAnswer,
                CreatorId = creatorId,
                IsFromSystem = true,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            var a = new List<int>();

            // --- BẮT ĐẦU PHẦN ÁNH XẠ VÀ VALIDATION CHO CÁC MỐI QUAN HỆ ---

            // 3. Ánh xạ và Validate Category IDs
            if (request.CategoryIds?.Any() == true)
            {
                // 3a. Validate: Kiểm tra từng ID (Nên tối ưu thành 1 Query như đã thảo luận)
                a = await _unitOfWork.Categories.GetNonExistingCategoryIdsAsync(request.CategoryIds);
                if (a.Any())
                    throw new NotFoundException($"Category ID {string.Join(", ", a)} không tồn tại.");


                // 3b. ÁNH XẠ: Tạo Collection các QuestionCategory
                question.QuestionCategories = request.CategoryIds
                    .Select(cId => new QuestionCategory
                    {
                        CategoryId = cId,
                        Question = question // Liên kết ngược (nếu cần thiết cho EF Core)
                    })
                    .ToList();
            }

            // 4. Ánh xạ và Validate Skill IDs
            if (request.SkillIds?.Any() == true)
            {
                a = await _unitOfWork.Skills.GetNonExistingSkillIdsAsync(request.SkillIds);
                if (a.Any())
                    throw new NotFoundException($"Skill ID {string.Join(", ", a)} không tồn tại.");


                // 4b. ÁNH XẠ: Tạo Collection các QuestionSkill
                question.QuestionSkills = request.SkillIds
                    .Select(sId => new QuestionSkill
                    {
                        SkillId = sId,
                        Question = question
                    })
                    .ToList();
            }

            // 5. Ánh xạ và Validate Position IDs
            if (request.PositionIds?.Any() == true)
            {
                a = await _unitOfWork.Positions.GetNonExistingPositionIdsAsync(request.PositionIds);
                if (a.Any())
                    throw new NotFoundException($"Position ID {string.Join(", ", a)} không tồn tại.");


                // 5b. ÁNH XẠ: Tạo Collection các QuestionPosition
                question.QuestionPositions = request.PositionIds
                    .Select(pId => new QuestionPosition
                    {
                        PositionId = pId,
                        Question = question
                    })
                    .ToList();
            }

            // --- KẾT THÚC ÁNH XẠ ---

            // 6. Lưu vào DB (EF Core sẽ tự động thêm Question và tất cả các Collection con)
            var created = await _unitOfWork.Questions.CreateSystemQuestionForStaffAsync(question);

            return created;
        }
        public async Task<GetAllSystemQuestionsForStaffAsyncResponse> GetSystemQuestionByIdAsync(int questionId, int? accountId)
        {
            var a = await _unitOfWork.Questions.GetQuestionByIdAsync(questionId, true);
            if (a == null) throw new NotFoundException($"Không tìm được câu hỏi hệ thống");
            var newa = new GetAllSystemQuestionsForStaffAsyncResponse
            {
                Id = a.Id,
                Content = a.Content,
                Difficulty = a.Difficulty,
                SampleAnswer = a.SampleAnswer,
                IsFromSystem = a.IsFromSystem,
                IsActive = a.IsActive,
                CreatorId = a.CreatorId,
                CreatorName = a.Creator?.FullName ?? "Unknown",
                CategoriesName = a.QuestionCategories.Select(c => c.Category.Name).ToList(),
                SkillsName = a.QuestionSkills.Select(s => s.Skill.Name).ToList(),
                PositionsName = a.QuestionPositions.Select(p => p.Position.Name).ToList(),
                Comments = a.Comments.Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    UserId = c.UserId,
                    UserName = c.User?.FullName ?? "Unknown",
                    UserAvatarUrl = c.User?.AvatarUrl ?? string.Empty,
                    UserRole = c.User?.AccountRoles?.FirstOrDefault()?.Role?.Name.ToString() ?? "Candidate",
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    UpvoteCount = c.Votes?.Count(v => v.IsUpvote) ?? 0,
                    DownvoteCount = c.Votes?.Count(v => !v.IsUpvote) ?? 0,
                    TotalVotes = c.Votes?.Count ?? 0,
                    CurrentUserVoteIsUpvote = accountId.HasValue ? c.Votes?.FirstOrDefault(v => v.AccountId == accountId.Value)?.IsUpvote : null
                }).ToList()
                // THAY THẾ .ToListAsync() bằng .FirstOrDefaultAsync() để chỉ lấy 1 đối tượng.


            };
            return newa;
        }
        public async Task<Question> UpdateSystemQuestionForStaffAsync(int questionId, UpdateSystemQuestionForStaffRequest request)
        {
            // 1. Tìm câu hỏi gốc
            var questionToUpdate = await _unitOfWork.Questions.GetQuestionByIdAsync(questionId);
            if (questionToUpdate == null)
            {
                throw new NotFoundException($"Không tìm thấy câu hỏi hệ thống với ID {questionId}.");
            }

            // Check trùng content, loại trừ ID hiện tại
            var existingContents = await _unitOfWork.Questions.FindExistingContentsAsync(new List<string> { request.Content });
            if (existingContents.Any(c => c.Equals(request.Content, StringComparison.OrdinalIgnoreCase)) && 
                !questionToUpdate.Content.Equals(request.Content, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException("Nội dung câu hỏi đã tồn tại trong hệ thống.");
            }

            // 2. KIỂM TRA SỰ TỒN TẠI CỦA ID (Phiên bản nâng cấp)
            // Thay thế logic cũ bằng logic mới ở đây

            // --- Kiểm tra Categories ---
            var nonExistingCategoryIds = await _unitOfWork.Categories.GetNonExistingCategoryIdsAsync(request.CategoryIds);
            if (nonExistingCategoryIds.Any())
            {
                var invalidIdsString = string.Join(", ", nonExistingCategoryIds);
                throw new BadRequestException($"Các CategoryId sau không tồn tại: {invalidIdsString}.");
            }

            // --- Kiểm tra Skills ---
            var nonExistingSkillIds = await _unitOfWork.Skills.GetNonExistingSkillIdsAsync(request.SkillIds);
            if (nonExistingSkillIds.Any())
            {
                var invalidIdsString = string.Join(", ", nonExistingSkillIds);
                throw new BadRequestException($"Các SkillId sau không tồn tại: {invalidIdsString}.");
            }

            // --- Kiểm tra Positions ---
            var nonExistingPositionIds = await _unitOfWork.Positions.GetNonExistingPositionIdsAsync(request.PositionIds);
            if (nonExistingPositionIds.Any())
            {
                var invalidIdsString = string.Join(", ", nonExistingPositionIds);
                throw new BadRequestException($"Các PositionId sau không tồn tại: {invalidIdsString}.");
            }

            // 3. Cập nhật các thuộc tính và quan hệ
            // (Giữ nguyên logic mapping và update relationship)
            questionToUpdate.Content = request.Content;
            questionToUpdate.SampleAnswer = request.SampleAnswer;
            questionToUpdate.Difficulty = request.Difficulty;
            questionToUpdate.IsActive = request.IsActive;
            questionToUpdate.UpdatedAt = DateTime.UtcNow;
            UpdateQuestionRelationships(questionToUpdate, request, questionId);

            // 4. Lưu thay đổi
            await _unitOfWork.Questions.UpdateQuestionAsync(questionToUpdate);



            // 5. Trả về đối tượng đã cập nhật khi thành công
            return questionToUpdate;
        }
        // Tách logic cập nhật quan hệ ra một hàm riêng cho sạch sẽ
        private void UpdateQuestionRelationships(Question questionToUpdate, UpdateSystemQuestionForStaffRequest request, int questionId)
        {
            // --- Categories ---
            var categoryIdsInRequest = request.CategoryIds ?? new List<int>();

            // SỬA LỖI Ở ĐÂY:
            // 1. Tìm tất cả các mục cần xóa
            var categoriesToRemove = questionToUpdate.QuestionCategories
                .Where(qc => !categoryIdsInRequest.Contains(qc.CategoryId))
                .ToList();

            // 2. Duyệt qua danh sách tạm và xóa khỏi collection gốc
            foreach (var categoryToRemove in categoriesToRemove)
            {
                questionToUpdate.QuestionCategories.Remove(categoryToRemove);
            }

            // Phần thêm mới giữ nguyên
            var currentCategoryIds = questionToUpdate.QuestionCategories.Select(qc => qc.CategoryId).ToList();
            var categoryIdsToAdd = categoryIdsInRequest.Except(currentCategoryIds).ToList();
            foreach (var categoryId in categoryIdsToAdd)
            {
                questionToUpdate.QuestionCategories.Add(new QuestionCategory { QuestionId = questionId, CategoryId = categoryId });
            }

            // --- Skills (áp dụng cách sửa tương tự) ---
            var skillIdsInRequest = request.SkillIds ?? new List<int>();

            var skillsToRemove = questionToUpdate.QuestionSkills
                .Where(qs => !skillIdsInRequest.Contains(qs.SkillId))
                .ToList();

            foreach (var skillToRemove in skillsToRemove)
            {
                questionToUpdate.QuestionSkills.Remove(skillToRemove);
            }

            var currentSkillIds = questionToUpdate.QuestionSkills.Select(qs => qs.SkillId).ToList();
            var skillIdsToAdd = skillIdsInRequest.Except(currentSkillIds).ToList();
            foreach (var skillId in skillIdsToAdd)
            {
                questionToUpdate.QuestionSkills.Add(new QuestionSkill { QuestionId = questionId, SkillId = skillId });
            }

            // --- Positions (áp dụng cách sửa tương tự) ---
            var positionIdsInRequest = request.PositionIds ?? new List<int>();

            var positionsToRemove = questionToUpdate.QuestionPositions
                .Where(qp => !positionIdsInRequest.Contains(qp.PositionId))
                .ToList();

            foreach (var positionToRemove in positionsToRemove)
            {
                questionToUpdate.QuestionPositions.Remove(positionToRemove);
            }

            var currentPositionIds = questionToUpdate.QuestionPositions.Select(qp => qp.PositionId).ToList();
            var positionIdsToAdd = positionIdsInRequest.Except(currentPositionIds).ToList();
            foreach (var positionId in positionIdsToAdd)
            {
                questionToUpdate.QuestionPositions.Add(new QuestionPosition { QuestionId = questionId, PositionId = positionId });
            }
        }

        public async Task<Question> UpdateContributedQuestionStatusAsync(int questionId, bool status, int staffId)
        {
            Question questionToUpdate = await _unitOfWork.Questions.GetQuestionByIdAsync(questionId);

            // Chỉ cho phép approve/reject khi câu hỏi đang ở trạng thái Pending
            if (questionToUpdate.ApprovalStatus != QuestionApprovalStatus.Pending)
            {
                throw new BadRequestException("Chỉ có thể approve/reject các câu hỏi đang chờ duyệt.");
            }

            var oldValue = new { IsActive = questionToUpdate.IsActive, ApprovalStatus = questionToUpdate.ApprovalStatus?.ToString() };
            var wasActive = questionToUpdate.IsActive;

            // Cập nhật ApprovalStatus và IsActive
            questionToUpdate.ApprovalStatus = status ? QuestionApprovalStatus.Approved : QuestionApprovalStatus.Rejected;
            questionToUpdate.IsActive = status;
            questionToUpdate.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Questions.UpdateQuestionAsync(questionToUpdate);

            var newValue = new { IsActive = questionToUpdate.IsActive, ApprovalStatus = questionToUpdate.ApprovalStatus?.ToString() };
            var action = status ? AuditAction.ApproveQuestion : AuditAction.RejectQuestion;
            await _auditLogService.CreateAuditLogAsync(staffId, action, "Question", questionId, oldValue, newValue);

            // Publish events để gửi notification cho người đóng góp
            if (status)
            {
                await _systemNotificationService.CreateAndSendNotificationAsync(questionToUpdate.CreatorId, "Câu hỏi đóng góp của bạn đã được chấp nhận", null);
            }
            else
            {
                await _systemNotificationService.CreateAndSendNotificationAsync(questionToUpdate.CreatorId, "Câu hỏi đóng góp của bạn đã bị từ chối", null);
            }

            return questionToUpdate;
        }

        // Method riêng để chỉ toggle IsActive (không đổi ApprovalStatus)
        // Dùng cho màn "Câu hỏi đóng góp" khi staff muốn ẩn/hiện câu hỏi đã được approve
        public async Task<Question> ToggleQuestionActiveStatusAsync(int questionId, bool isActive, int staffId)
        {
            Question questionToUpdate = await _unitOfWork.Questions.GetQuestionByIdAsync(questionId);

            // Chỉ cho phép toggle nếu câu hỏi đã được approve
            if (questionToUpdate.ApprovalStatus != QuestionApprovalStatus.Approved)
            {
                throw new BadRequestException("Chỉ có thể toggle IsActive cho các câu hỏi đã được approve.");
            }

            var oldValue = new { IsActive = questionToUpdate.IsActive };
            questionToUpdate.IsActive = isActive;
            questionToUpdate.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Questions.UpdateQuestionAsync(questionToUpdate);

            var newValue = new { IsActive = questionToUpdate.IsActive };
            // Tạo audit log cho việc toggle (action Update)
            await _auditLogService.CreateAuditLogAsync(staffId, AuditAction.Update, "Question", questionId, oldValue, newValue);

            return questionToUpdate;
        }

        public async Task<List<QuestionValidationResponse>> ValidateQuestionsFromExcelAsync(IFormFile file)
        {
            var validationResults = new List<QuestionValidationResponse>();
            var fileExtension = Path.GetExtension(file.FileName);
            if (fileExtension?.ToLower() != ".xlsx")
            {
                throw new Exception("Định dạng file không hợp lệ. Chỉ chấp nhận file Excel (.xlsx).");
            }
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var worksheet = workbook.Worksheet(1);
                    if (worksheet == null || worksheet.IsEmpty())
                    {
                        throw new Exception("File Excel trống hoặc không có worksheet hợp lệ.");
                    }

                    // --- BƯỚC KIỂM TRA HEADER ---
                    var firstRow = worksheet.FirstRowUsed();
                    if (firstRow == null)
                    {
                        throw new Exception("File Excel không có dữ liệu hoặc không có dòng tiêu đề.");
                    }

                    // Đọc các giá trị từ các ô trong dòng đầu tiên
                    var actualHeaders = firstRow.Cells(1, ExpectedHeaders.Count)
                                              .Select(cell => cell.GetValue<string>().Trim())
                                              .ToList();

                    // So sánh danh sách header thực tế với danh sách mong đợi (không phân biệt hoa/thường)
                    if (!ExpectedHeaders.SequenceEqual(actualHeaders, StringComparer.OrdinalIgnoreCase))
                    {
                        var expectedFormat = string.Join(", ", ExpectedHeaders);
                        var actualFormat = string.Join(", ", actualHeaders);

                        // Ném ra một Exception với thông báo lỗi rõ ràng
                        throw new Exception($"Định dạng file Excel không hợp lệ. " +
                                            $"Dòng tiêu đề phải là: '{expectedFormat}'. " +
                                            $"Định dạng hiện tại của bạn là: '{actualFormat}'.");
                    }
                    // --- KẾT THÚC BƯỚC KIỂM TRA HEADER ---

                    var rows = worksheet.RowsUsed().Skip(1); // Bỏ qua dòng header

                    // --- GIAI ĐOẠN 1: THU THẬP TẤT CẢ ID TỪ FILE EXCEL ---
                    // Mục đích: Gom hết ID lại để truy vấn DB một lần duy nhất, tránh N+1 query.
                    var allCategoryNames = new HashSet<string>();
                    var allSkillNames = new HashSet<string>();
                    var allPositionNames = new HashSet<string>();
                    var allContents = new List<string>(); // <-- THÊM MỚI

                    foreach (var row in rows)
                    {
                        // Lấy Content và thêm vào list, nhớ Trim() để loại bỏ khoảng trắng thừa
                        var content = row.Cell(1).GetValue<string>().Trim();
                        if (!string.IsNullOrEmpty(content))
                        {
                            allContents.Add(content);
                        }

                        ParseNames(row.Cell(4).GetValue<string>()).ForEach(name => allCategoryNames.Add(name));
                        ParseNames(row.Cell(5).GetValue<string>()).ForEach(name => allSkillNames.Add(name));
                        ParseNames(row.Cell(6).GetValue<string>()).ForEach(name => allPositionNames.Add(name));
                    }

                    // --- GIAI ĐOẠN 2: VALIDATE HÀNG LOẠT VỚI DATABASE ---
                    // Gọi DB 3 lần để lấy về tất cả các ID không tồn tại tương ứng.
                    var nonExistentCategoryNames = await _unitOfWork.Categories.GetNonExistingCategoryNames(allCategoryNames.ToList());
                    var nonExistentSkillNames = await _unitOfWork.Skills.GetNonExistingSkillNames(allSkillNames.ToList());
                    var nonExistentPositionNames = await _unitOfWork.Positions.GetNonExistingPositionNames(allPositionNames.ToList());
                    // --> GỌI HÀM MỚI: Hỏi DB 1 lần duy nhất xem content nào đã tồn tại
                    var existingContentsInDb = await _unitOfWork.Questions.FindExistingContentsAsync(allContents);

                    // --- GIAI ĐOẠN 3: KIỂM TRA TỪNG DÒNG VÀ GÁN KẾT QUẢ ---
                    // Vòng lặp này không còn gọi DB nữa, chỉ so sánh với dữ liệu đã lấy ở Giai đoạn 2.

                    // Dùng HashSet để kiểm tra trùng lặp ngay trong file Excel (cực nhanh)
                    var seenContentsInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var row in rows)
                    {
                        var result = new QuestionValidationResponse { RowIndex = row.RowNumber() };

                        // Đọc dữ liệu từ các ô
                        result.Content = row.Cell(1).GetValue<string>();
                        result.Difficulty = row.Cell(2).GetValue<string>();
                        result.SampleAnswer = row.Cell(3).GetValue<string>();
                        result.CategoryNames = row.Cell(4).GetValue<string>();
                        result.SkillNames = row.Cell(5).GetValue<string>();
                        result.PositionNames = row.Cell(6).GetValue<string>();

                        // --- KIỂM TRA LỖI CHO TRƯỜNG CONTENT ---
                        if (string.IsNullOrWhiteSpace(result.Content))
                        {
                            result.IsValid = false;
                            result.Errors["Content"] = "Nội dung câu hỏi không được để trống.";
                        }
                        else
                        {
                            // 1. Kiểm tra trùng với DB
                            if (existingContentsInDb.Contains(result.Content))
                            {
                                result.IsValid = false;
                                result.Errors["Content"] = "Nội dung câu hỏi đã tồn tại trong hệ thống.";
                            }
                            // 2. Kiểm tra trùng ngay trong file
                            // Dùng else if để không ghi đè lỗi ở trên
                            else if (!seenContentsInFile.Add(result.Content)) // .Add() trả về false nếu đã tồn tại
                            {
                                result.IsValid = false;
                                result.Errors["Content"] = "Nội dung câu hỏi bị lặp lại trong file Excel này.";
                            }
                        }

                        if (!Enum.TryParse<DifficultyLevel>(result.Difficulty, true, out _))
                        {
                            result.IsValid = false;
                            result.Errors["Difficulty"] = "Mức độ không hợp lệ (Phải là Easy, Medium, Hard).";
                        }
                        // 1. Kiểm tra Category Names cho dòng hiện tại
                        var categoryNamesInRow = ParseNames(result.CategoryNames);
                        var invalidCatNames = categoryNamesInRow.Where(name => nonExistentCategoryNames.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList();
                        if (invalidCatNames.Any())
                        {
                            result.IsValid = false;
                            result.Errors["CategoryNames"] = $"Các tên thể loại không tồn tại: {string.Join(", ", invalidCatNames)}";
                        }

                        // 2. Kiểm tra Skill Names cho dòng hiện tại
                        var skillNamesInRow = ParseNames(result.SkillNames);
                        var invalidSkillNames = skillNamesInRow.Where(name => nonExistentSkillNames.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList();
                        if (invalidSkillNames.Any())
                        {
                            result.IsValid = false;
                            result.Errors["SkillNames"] = $"Các tên kĩ năng không tồn tại: {string.Join(", ", invalidSkillNames)}";
                        }

                        // 3. Kiểm tra Position Names cho dòng hiện tại
                        var positionNamesInRow = ParseNames(result.PositionNames);
                        var invalidPositionNames = positionNamesInRow.Where(name => nonExistentPositionNames.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList();
                        if (invalidPositionNames.Any())
                        {
                            result.IsValid = false;
                            result.Errors["PositionNames"] = $"Các tên vị trí không tồn tại: {string.Join(", ", invalidPositionNames)}";
                        }


                        validationResults.Add(result);
                    }
                }
            }
            return validationResults;
        }

        public async Task<int> CreateValidatedQuestionsAsync(List<FinalImportRequest> requests, int creatorId)
        {
            // --- BƯỚC 1: KIỂM TRA ĐIỀU KIỆN BAN ĐẦU ---
            if (await _unitOfWork.Accounts.AreUsersExisted(creatorId) == false)
                throw new NotFoundException($"Creator ID {creatorId} không tồn tại.");

            // --- BƯỚC 2: TẠO BẢN ĐỒ ÁNH XẠ TÊN -> ID ĐỂ TĂNG TỐC ---
            // Mục đích: Chỉ truy vấn DB một lần duy nhất cho mỗi loại (Category, Skill, Position)

            // 2a. Thu thập tất cả các tên duy nhất từ toàn bộ request
            var allCategoryNames = requests.SelectMany(r => ParseNames(r.CategoryNames)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var allSkillNames = requests.SelectMany(r => ParseNames(r.SkillNames)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var allPositionNames = requests.SelectMany(r => ParseNames(r.PositionNames)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            // 2b. Lấy các object tương ứng từ DB
            var existingCategories = await _unitOfWork.Categories.FindCategoriesByNamesAsync(allCategoryNames);
            var existingSkills = await _unitOfWork.Skills.FindSkillsByNamesAsync(allSkillNames);
            var existingPositions = await _unitOfWork.Positions.FindPositionsByNamesAsync(allPositionNames);

            // 2c. Tạo Dictionary để tra cứu ID từ tên cực nhanh (O(1))
            var categoryNameToIdMap = existingCategories.ToDictionary(c => c.Name, c => c.Id, StringComparer.OrdinalIgnoreCase);
            var skillNameToIdMap = existingSkills.ToDictionary(s => s.Name, s => s.Id, StringComparer.OrdinalIgnoreCase);
            var positionNameToIdMap = existingPositions.ToDictionary(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);

            // --- BƯỚC 3: KIỂM TRA TRÙNG LẶP CONTENT LẦN CUỐI ---
            // Để tránh race condition khi hai người import cùng lúc
            var allContents = requests.Select(r => r.Content).Where(c => !string.IsNullOrEmpty(c)).ToList();
            var existingContentsInDb = await _unitOfWork.Questions.FindExistingContentsAsync(allContents);

            // --- BƯỚC 4: TẠO CÁC ENTITY ---
            var questionsToCreate = new List<Question>();
            foreach (var request in requests)
            {
                // Bỏ qua nếu content rỗng hoặc đã tồn tại (trong DB hoặc trong chính request này)
                if (string.IsNullOrWhiteSpace(request.Content) || !existingContentsInDb.Add(request.Content))
                {
                    continue;
                }

                if (!Enum.TryParse<DifficultyLevel>(request.Difficulty, true, out var difficulty))
                {
                    difficulty = DifficultyLevel.Easy; // Gán giá trị mặc định nếu có lỗi
                }

                var question = new Question
                {
                    Content = request.Content,
                    Difficulty = difficulty,
                    SampleAnswer = request.SampleAnswer,
                    CreatorId = creatorId,
                    IsFromSystem = true,
                    IsActive = true
                    // CreatedDate và UpdatedDate sẽ được tự động gán bởi BaseEntity
                };

                // Lấy ID từ các bản đồ ánh xạ đã tạo
                var categoryIds = ParseNames(request.CategoryNames)
                                    .Select(name => categoryNameToIdMap.GetValueOrDefault(name))
                                    .Where(id => id != 0); // Lọc ra những tên không tìm thấy

                var skillIds = ParseNames(request.SkillNames)
                                    .Select(name => skillNameToIdMap.GetValueOrDefault(name))
                                    .Where(id => id != 0);

                var positionIds = ParseNames(request.PositionNames)
                                    .Select(name => positionNameToIdMap.GetValueOrDefault(name))
                                    .Where(id => id != 0);

                // Tạo các thực thể quan hệ (join entities)
                question.QuestionCategories = categoryIds.Select(cId => new QuestionCategory { CategoryId = cId }).ToList();
                question.QuestionSkills = skillIds.Select(sId => new QuestionSkill { SkillId = sId }).ToList();
                question.QuestionPositions = positionIds.Select(pId => new QuestionPosition { PositionId = pId }).ToList();

                questionsToCreate.Add(question);
            }

            // --- BƯỚC 5: LƯU VÀO DATABASE ---
            if (questionsToCreate.Any())
            {
                await _unitOfWork.Questions.CreateBulkAsync(questionsToCreate);
            }

            return questionsToCreate.Count;
        }
        // Hàm helper để chuyển chuỗi "1, 2, 3" thành List<int>
        private List<int> ParseIds(string idsText)
        {
            if (string.IsNullOrWhiteSpace(idsText))
            {
                return new List<int>();
            }
            return idsText.Split(',')
                          .Where(idStr => int.TryParse(idStr.Trim(), out _))
                          .Select(idStr => int.Parse(idStr.Trim()))
                          .ToList();
        }
        private (List<int> ValidIds, List<string> InvalidParts) ValidateAndParseIds(string idsText)
        {
            if (string.IsNullOrWhiteSpace(idsText))
            {
                return (new List<int>(), new List<string>());
            }

            var validIds = new List<int>();
            var invalidParts = new List<string>();
            var parts = idsText.Split(',');

            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                if (string.IsNullOrEmpty(trimmedPart)) continue; // Bỏ qua nếu là khoảng trắng thừa giữa dấu phẩy

                if (int.TryParse(trimmedPart, out int id))
                {
                    validIds.Add(id);
                }
                else
                {
                    invalidParts.Add(trimmedPart);
                }
            }

            return (validIds, invalidParts);
        }
        // Hàm helper mới để parse tên từ chuỗi "Java, C#, React"
        private List<string> ParseNames(string namesText)
        {
            if (string.IsNullOrWhiteSpace(namesText))
            {
                return new List<string>();
            }
            return namesText.Split(',')
                          .Select(name => name.Trim())
                          .Where(name => !string.IsNullOrEmpty(name))
                          .ToList();
        }
        public async Task<QuestionValidationResponse> RevalidateSingleQuestionAsync(FinalImportRequest request)
        {

            var validationResult = new QuestionValidationResponse
            {
                RowIndex = 0,
                Content = request.Content,
                Difficulty = request.Difficulty,
                SampleAnswer = request.SampleAnswer,
                CategoryNames = request.CategoryNames,
                SkillNames = request.SkillNames,
                PositionNames = request.PositionNames
            };

            var categoryNames = ParseNames(request.CategoryNames);
            var skillNames = ParseNames(request.SkillNames);
            var positionNames = ParseNames(request.PositionNames);

            var nonExistentCategoryNames = await _unitOfWork.Categories.GetNonExistingCategoryNames(categoryNames);
            var nonExistentSkillNames = await _unitOfWork.Skills.GetNonExistingSkillNames(skillNames);
            var nonExistentPositionNames = await _unitOfWork.Positions.GetNonExistingPositionNames(positionNames);

            var allContents = new List<string> { request.Content };
            var existingContentsInDb = await _unitOfWork.Questions.FindExistingContentsAsync(allContents);


            validationResult.IsValid = true;

            if (string.IsNullOrWhiteSpace(validationResult.Content))
            {
                validationResult.IsValid = false;
                validationResult.Errors["Content"] = "Nội dung câu hỏi không được để trống.";
            }
            else if (existingContentsInDb.Contains(validationResult.Content, StringComparer.OrdinalIgnoreCase))
            {
                validationResult.IsValid = false;
                validationResult.Errors["Content"] = "Nội dung câu hỏi đã tồn tại trong hệ thống.";
            }

            if (!Enum.TryParse<DifficultyLevel>(validationResult.Difficulty, true, out _))
            {
                validationResult.IsValid = false;
                validationResult.Errors["Difficulty"] = "Mức độ không hợp lệ (Phải là Easy, Medium, Hard).";
            }

            if (nonExistentCategoryNames.Any())
            {
                validationResult.IsValid = false;
                validationResult.Errors["CategoryNames"] = $"Các tên thể loại không tồn tại: {string.Join(", ", nonExistentCategoryNames)}";
            }

            if (nonExistentSkillNames.Any())
            {
                validationResult.IsValid = false;
                validationResult.Errors["SkillNames"] = $"Các tên kĩ năng không tồn tại: {string.Join(", ", nonExistentSkillNames)}";
            }

            if (nonExistentPositionNames.Any())
            {
                validationResult.IsValid = false;
                validationResult.Errors["PositionNames"] = $"Các tên vị trí không tồn tại: {string.Join(", ", nonExistentPositionNames)}";
            }

            return validationResult;
        }

        public async Task<PagedList<GetMyContributedQuestionsResponse>> GetMyContributedQuestionsAsync(int accountId, GetMyContributedQuestionsParams questionParams)
        {
            var query = _unitOfWork.Questions.GetMyContributedQuestions(accountId);

            // 1. Filter theo SearchTerm (trên Content)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm));
            }

            // 2. Filter theo ApprovalStatus
            if (questionParams.ApprovalStatus.HasValue)
            {
                query = query.Where(q => q.ApprovalStatus == questionParams.ApprovalStatus.Value);
            }

            // 3. Filter theo Skill ID
            if (questionParams.SkillId.HasValue)
            {
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 4. Filter theo Position ID
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }

            // 5. Filter theo Category ID
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == questionParams.CategoryId.Value));
            }

            // 6. Filter theo Level
            if (questionParams.Level.HasValue)
            {
                query = query.Where(q => q.ContributedDetail != null && q.ContributedDetail.Level == questionParams.Level.Value);
            }

            // --- LOGIC SẮP XẾP (SORTING) ---
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";

                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
                        ? query.OrderByDescending(q => q.Content.Substring(0, 1).ToLower())
                        : query.OrderBy(q => q.Content.Substring(0, 1).ToLower()),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),
                    "updatedat" => isDescending
                        ? query.OrderByDescending(q => q.UpdatedAt ?? q.CreatedAt)
                        : query.OrderBy(q => q.UpdatedAt ?? q.CreatedAt),
                    _ => query.OrderByDescending(q => q.CreatedAt)
                };
            }
            else
            {
                // Sắp xếp mặc định: mới nhất trước
                query = query.OrderByDescending(q => q.CreatedAt);
            }

            var response = query.Select(q => new GetMyContributedQuestionsResponse
            {
                Id = q.Id,
                Content = q.Content ?? string.Empty,
                IsActive = q.IsActive,
                ApprovalStatus = q.ApprovalStatus.HasValue ? q.ApprovalStatus.Value.ToString() : null,
                SampleAnswer = q.SampleAnswer,
                Difficulty = q.Difficulty.ToString(),
                ContributedDetailId = q.ContributedDetailId,
                ContributedDetail = q.ContributedDetail != null ? new MyContributedDetailDto
                {
                    Id = q.ContributedDetail.Id,
                    InterviewDate = q.ContributedDetail.InterviewDate,
                    Level = q.ContributedDetail.Level.ToString(),
                    Company = q.ContributedDetail.Company != null ? new CompanyDto
                    {
                        Id = q.ContributedDetail.Company.Id,
                        Name = q.ContributedDetail.Company.Name,
                        ImageUrl = q.ContributedDetail.Company.ImageUrl
                    } : null
                } : null,
                CategoriesName = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                SkillsName = q.QuestionSkills.Select(qs => qs.Skill.Name ?? string.Empty).ToList(),
                PositionsName = q.QuestionPositions.Select(qp => qp.Position.Name ?? string.Empty).ToList(),
                CreatedAt = q.CreatedAt,
                UpdatedAt = q.UpdatedAt
            });

            return await PagedList<GetMyContributedQuestionsResponse>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);
        }

        public async Task<byte[]> ExportSystemQuestionsToExcelAsync(GetSystemQuestionParams questionParams)
        {
            // Lấy tất cả câu hỏi hệ thống (không phân trang)
            var allQuestions = await GetAllSystemQuestionsForStaffAsync(new GetSystemQuestionParams
            {
                PageNumber = 1,
                PageSize = int.MaxValue, // Lấy tất cả
                SearchTerm = questionParams.SearchTerm,
                IsActive = questionParams.IsActive,
                SkillId = questionParams.SkillId,
                PositionId = questionParams.PositionId,
                CategoryId = questionParams.CategoryId,
                Difficulty = questionParams.Difficulty,
                SortBy = questionParams.SortBy
            });

            return ExcelTemplateGenerator.GenerateSystemQuestionsExport(allQuestions.Items);
        }

        /// <summary>
        /// Get positions and skills from questions that are related to a specific company
        /// Used for manual interview setup - filter positions/skills based on available questions
        /// </summary>
        public async Task<CompanyPositionsSkillsResponse> GetPositionsAndSkillsByCompanyAsync(int companyId)
        {
            // Query questions that:
            // 1. Are contributed questions (not system questions) - because only contributed questions have company
            // 2. Are approved and active
            // 3. Belong to the specified company
            var questions = await _context.Questions
                .Where(q => !q.IsFromSystem // Only contributed questions have company
                    && q.IsActive
                    && q.ApprovalStatus == QuestionApprovalStatus.Approved
                    && q.ContributedDetail != null
                    && q.ContributedDetail.CompanyId == companyId)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .AsNoTracking()
                .ToListAsync();

            // Extract distinct positions
            var distinctPositions = questions
                .SelectMany(q => q.QuestionPositions)
                .Select(qp => qp.Position)
                .Where(p => p != null && p.IsActive)
                .GroupBy(p => p.Id)
                .Select(g => g.First())
                .Select(p => new PositionForCompanyResponse
                {
                    Id = p.Id,
                    Name = p.Name
                })
                .OrderBy(p => p.Name)
                .ToList();

            // Extract distinct skills
            var distinctSkills = questions
                .SelectMany(q => q.QuestionSkills)
                .Select(qs => qs.Skill)
                .Where(s => s != null && s.IsActive)
                .GroupBy(s => s.Id)
                .Select(g => g.First())
                .Select(s => new SkillForCompanyResponse
                {
                    Id = s.Id,
                    Name = s.Name
                })
                .OrderBy(s => s.Name)
                .ToList();

            return new CompanyPositionsSkillsResponse
            {
                Positions = distinctPositions,
                Skills = distinctSkills
            };
        }

    }
}
