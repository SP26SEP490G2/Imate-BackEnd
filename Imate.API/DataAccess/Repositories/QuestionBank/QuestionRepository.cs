using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Imate.API.DataAccess.Repositories.QuestionBank
{
    public class QuestionRepository : RepositoryBase<Question>, IQuestionRepository
    {
        private readonly ImateDbContext _context;

        public QuestionRepository(ImateDbContext repositoryContext)
          : base(repositoryContext)
        {
        }

        public IQueryable<Question> GetAllSystemQuestionsForStaff()
        {
            return _context.Questions
            .Include(q => q.Creator)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                    .Where(q => q.IsFromSystem == true).AsNoTracking();
        }

        public IQueryable<Question> GetAllContributedForStaffQuestions()
        {
            return _context.Questions
                .Include(q => q.Creator)
                .Include(q => q.QuestionCategories).ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills).ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions).ThenInclude(qp => qp.Position)
                .Include(q => q.ContributedDetail).ThenInclude(cd => cd.Company)
                .Where(q => q.IsFromSystem == false).AsNoTracking();
        }

        public async Task<IEnumerable<Question>> GetAllContributedQuestionsWithRelatedDataAsync()
        {
            var questions = await _context.Questions
            .Where(q => q.IsFromSystem == false && q.IsActive)
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
            .Include(q => q.Comments)
                .ThenInclude(c => c.User)
                    .ThenInclude(u => u.AccountRoles)
                        .ThenInclude(ar => ar.Role)
            .Include(q => q.Comments)
                .ThenInclude(c => c.Votes)
                .AsSplitQuery()
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

            // Sắp xếp lại trong memory để đảm bảo thứ tự (mới nhất trước)
            return questions.OrderByDescending(q => q.CreatedAt);
        }
        //
        public async Task<Question> GetQuestionByIdWithRelatedDataAsync(int questionId)
        {
            return await _context.Questions
                .Where(q => q.Id == questionId && q.IsFromSystem == false)
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
            .Include(q => q.Comments)
                .ThenInclude(c => c.User)
                    .ThenInclude(u => u.AccountRoles)
                        .ThenInclude(ar => ar.Role)
            .Include(q => q.Comments)
                .ThenInclude(c => c.Votes)
                .AsSplitQuery()
                .FirstOrDefaultAsync();
        }
        public IQueryable<Question> GetAllQuestions()
        {
            return _context.Questions.
                Include(q => q.QuestionCategories).ThenInclude(q => q.Category)
                .Include(q => q.QuestionSkills).ThenInclude(q => q.Skill)
                .Include(q => q.QuestionPositions).ThenInclude(q => q.Position)
                .AsNoTracking();
        }
        public IQueryable<Question> GetAllQuestionsTracking()
        {
            return _context.Questions.
                Include(q => q.QuestionCategories).ThenInclude(q => q.Category)
                .Include(q => q.QuestionSkills).ThenInclude(q => q.Skill)
                .Include(q => q.QuestionPositions).ThenInclude(q => q.Position);

        }

        public async Task<IEnumerable<Question>> GetPublicSystemQuestionBanksAsync()
        {
            var questions = await _context.Questions
                .Where(q => q.IsFromSystem && q.IsActive)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            // Sắp xếp lại trong memory để đảm bảo thứ tự (mới nhất trước)
            return questions.OrderByDescending(q => q.CreatedAt);
        }


        //Candidate đóng góp câu hỏi
        public async Task<IEnumerable<Company>> GetCompaniesAsync()
        {
            return await _context.Companies.Where(c => c.IsActive).AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.Where(c => c.IsActive).AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<Position>> GetPositionsWithSkillsAsync()
        {
            return await _context.Positions
                .Where(p => p.IsActive)
                .AsNoTracking()
                .Include(p => p.PositionSkills)
                .ThenInclude(ps => ps.Skill)
                .ToListAsync();
        }

        public async Task CreateContributedQuestionAsync(Question question)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                await _context.Questions.AddAsync(question);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task<Question> CreateSystemQuestionForStaffAsync(Question question)
        {
            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return question;
        }
        public async Task<Question> UpdateQuestionAsync(Question question)
        {
            // EF Core tự động theo dõi các thay đổi trên object 'question' đã được gắn (tracked)
            // nên chỉ cần gọi SaveChangesAsync là đủ.
            question.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return question;
        }

        public async Task<Question> GetQuestionByIdAsync(int questionId)
        {
            var a = await _context.Questions
                .Where(q => q.Id == questionId)
                .Include(q => q.Creator)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                          .FirstOrDefaultAsync();
            return a;

        }

        public async Task<IEnumerable<int>> GetSavedQuestionIdsByAccountAsync(int accountId)
        {
            return await _context.SavedQuestions
            .Where(sq => sq.AccountId == accountId)

            .Select(sq => sq.QuestionId)

            .ToListAsync();
        }
        public async Task<HashSet<string>> FindExistingContentsAsync(List<string> contents)
        {
            if (contents == null || !contents.Any())
            {
                return new HashSet<string>();
            }

            // Lấy về tất cả các Content từ DB mà khớp với danh sách đầu vào
            var existingContents = await _context.Questions
                .Where(q => contents.Contains(q.Content))
                .Select(q => q.Content)
                .ToListAsync();

            // Trả về một HashSet để tra cứu nhanh hơn, không phân biệt hoa thường
            return new HashSet<string>(existingContents, StringComparer.OrdinalIgnoreCase);
        }
        public async Task CreateBulkAsync(IEnumerable<Question> questions)
        {
            await _context.Questions.AddRangeAsync(questions);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateRangeAsync(IEnumerable<Question> questions)
        {
            _context.Questions.UpdateRange(questions);
            await _context.SaveChangesAsync();
        }
        public async Task SaveChange()
        {

            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<Question>> GetLimitedPublicSystemQuestionBanksAsync()
        {
            // Logic này sẽ lấy 10 câu hỏi MỚI NHẤT cho MỖI thể loại
            // (Yêu cầu EF Core 5.0+ để chạy GroupBy/SelectMany với Take)

            var questions = await _context.Categories
                .SelectMany(category =>
                    _context.Questions
                        // 1. Áp dụng bộ lọc cơ sở (GIỐNG HỆT phương thức gốc)
                        .Where(q => q.IsFromSystem && q.IsActive)

                        // 2. Lấy câu hỏi thuộc thể loại này
                        .Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == category.Id))

                        // 3. Sắp xếp (GIỐNG HỆT phương thức gốc)
                        .OrderByDescending(q => q.CreatedAt)

                        // 4. Áp dụng giới hạn 10 câu
                        .Take(5)
                )
                .Distinct() // Đảm bảo không trùng lặp nếu 1 câu thuộc nhiều thể loại

                // 5. Include các bảng liên quan (GIỐNG HỆT phương thức gốc)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)

                // 6. Sắp xếp lại kết quả cuối cùng (GIỐNG HỆT phương thức gốc)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            // Sắp xếp lại trong memory để đảm bảo thứ tự (mới nhất trước)
            return questions.OrderByDescending(q => q.CreatedAt);
        }
        public async Task<IEnumerable<Question>> GetLimitedContributedQuestionsWithRelatedDataAsync()
        {
            // Logic này sẽ lấy 5 câu hỏi MỚI NHẤT cho MỖI thể loại
            // (Yêu cầu EF Core 5.0+ để chạy SelectMany với Take)

            var questions = await _context.Categories
                .SelectMany(category =>
                    _context.Questions
                        // 1. Áp dụng bộ lọc cơ sở (GIỐNG HỆT phương thức gốc)
                        .Where(q => q.IsFromSystem == false && q.IsActive)

                        // 2. Lấy câu hỏi thuộc thể loại này
                        .Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == category.Id))

                        // 3. Sắp xếp (Giả sử OrderByDescending)
                        .OrderByDescending(q => q.CreatedAt)

                        // 4. Áp dụng giới hạn 5 câu
                        .Take(5)
                )
                .Distinct()
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
                .Include(q => q.Comments)
                    .ThenInclude(c => c.User)
                        .ThenInclude(u => u.AccountRoles)
                            .ThenInclude(ar => ar.Role)
                .Include(q => q.Comments)
                    .ThenInclude(c => c.Votes)
                .AsSplitQuery()
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            // Sắp xếp lại trong memory để đảm bảo thứ tự (mới nhất trước)
            return questions.OrderByDescending(q => q.CreatedAt);
        }

        public IQueryable<Question> GetMyContributedQuestions(int accountId)
        {
            return _context.Questions
                .Where(q => q.IsFromSystem == false && q.CreatorId == accountId)
                .Include(q => q.Creator)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .Include(q => q.ContributedDetail)
                    .ThenInclude(cd => cd.Company)
                .AsNoTracking();
        }

        public async Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync()
        {
            return await FindAll(trackChanges: false)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.Comments)
                .Where(q => q.IsActive)
                .OrderByDescending(q => q.Comments.Count)
                .Select(q => new QuestionResponse.ListHotQuestion
                {
                    Id = q.Id,
                    Content = q.Content,
                    Categories = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                    CommentCount = q.Comments.Count
                })
                .ToListAsync();
        }

        public async Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request)
        {
            // TODO: Uncomment this when you have real data
            /*
            var query = FindAll(trackChanges: false)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.Comments)
                .Include(q => q.Creator)
                .Where(q => q.IsActive);

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
            */

            // Fake data for testing
            await Task.Delay(100);

            var fakeQuestions = new List<QuestionResponse.QuestionBankItem>
            {
                new QuestionResponse.QuestionBankItem
                {
                    Id = 1,
                    Title = "React: Map/Reduce/Filter quy mo lon",
                    Content = "Cach su dung Map/Reduce/Filter trong React quy mo lon khi xu ly du lieu?",
                    Categories = new List<string> { "Frontend" },
                    Skills = new List<string> { "React", "JavaScript" },
                    Difficulty = "Easy",
                    CommentCount = 15,
                    CreatedBy = "System",
                    CreatedAt = DateTimeOffset.Now.AddDays(-5)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 2,
                    Title = "System Design: Real-time Chat",
                    Content = "Thiet ke he thong Real-time Chat voi 1 trieu CCU?",
                    Categories = new List<string> { "System Design" },
                    Skills = new List<string> { "WebSocket", "Redis", "Kafka" },
                    Difficulty = "Hard",
                    CommentCount = 56,
                    CreatedBy = "Admin",
                    CreatedAt = DateTimeOffset.Now.AddDays(-10)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 3,
                    Title = "Node.js: Event Loop",
                    Content = "Event Loop trong Node.js hoat dong the nao?",
                    Categories = new List<string> { "Backend" },
                    Skills = new List<string> { "Node.js" },
                    Difficulty = "Medium",
                    CommentCount = 12,
                    CreatedBy = "John Doe",
                    CreatedAt = DateTimeOffset.Now.AddDays(-3)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 4,
                    Title = "Database: Query Optimization",
                    Content = "Lam sao de toi uu hoa query trong SQL Server?",
                    Categories = new List<string> { "Database" },
                    Skills = new List<string> { "SQL", "Performance" },
                    Difficulty = "Medium",
                    CommentCount = 23,
                    CreatedBy = "Jane Smith",
                    CreatedAt = DateTimeOffset.Now.AddDays(-7)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 5,
                    Title = "React: State Management",
                    Content = "Redux vs Context API: Nen dung cai nao?",
                    Categories = new List<string> { "Frontend" },
                    Skills = new List<string> { "React", "Redux" },
                    Difficulty = "Easy",
                    CommentCount = 8,
                    CreatedBy = "Bob Johnson",
                    CreatedAt = DateTimeOffset.Now.AddDays(-1)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 6,
                    Title = "DevOps: CI/CD Pipeline",
                    Content = "Cach xay dung CI/CD pipeline voi Docker va Kubernetes?",
                    Categories = new List<string> { "DevOps" },
                    Skills = new List<string> { "Docker", "Kubernetes", "Jenkins" },
                    Difficulty = "Hard",
                    CommentCount = 34,
                    CreatedBy = "System",
                    CreatedAt = DateTimeOffset.Now.AddDays(-15)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 7,
                    Title = "Algorithm: Binary Search Tree",
                    Content = "Cach implement Binary Search Tree hieu qua?",
                    Categories = new List<string> { "Algorithm" },
                    Skills = new List<string> { "Data Structure", "Algorithm" },
                    Difficulty = "Medium",
                    CommentCount = 19,
                    CreatedBy = "Alice Brown",
                    CreatedAt = DateTimeOffset.Now.AddDays(-20)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 8,
                    Title = "Security: Authentication Best Practices",
                    Content = "Cac phuong phap bao mat tot nhat cho authentication?",
                    Categories = new List<string> { "Security" },
                    Skills = new List<string> { "JWT", "OAuth", "Security" },
                    Difficulty = "Hard",
                    CommentCount = 42,
                    CreatedBy = "Mike Wilson",
                    CreatedAt = DateTimeOffset.Now.AddDays(-12)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 9,
                    Title = "Backend: REST API Design",
                    Content = "Nguyen tac thiet ke REST API chuan?",
                    Categories = new List<string> { "Backend" },
                    Skills = new List<string> { "API", "REST" },
                    Difficulty = "Easy",
                    CommentCount = 11,
                    CreatedBy = "Sarah Davis",
                    CreatedAt = DateTimeOffset.Now.AddDays(-8)
                },
                new QuestionResponse.QuestionBankItem
                {
                    Id = 10,
                    Title = "Mobile: React Native Performance",
                    Content = "Toi uu performance cho React Native app?",
                    Categories = new List<string> { "Mobile" },
                    Skills = new List<string> { "React Native", "Performance" },
                    Difficulty = "Medium",
                    CommentCount = 17,
                    CreatedBy = "Tom Anderson",
                    CreatedAt = DateTimeOffset.Now.AddDays(-6)
                }
            };

            var filteredQuestions = fakeQuestions.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                filteredQuestions = filteredQuestions.Where(q =>
                    q.Content.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                    q.Title.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
            }

            // Apply category filter by ID
            if (request.CategoryId.HasValue)
            {
                var categoryName = request.CategoryId.Value switch
                {
                    1 => "Frontend",
                    2 => "Backend",
                    3 => "System Design",
                    4 => "Database",
                    5 => "DevOps",
                    6 => "Mobile",
                    7 => "Security",
                    8 => "Algorithm",
                    _ => null
                };

                if (categoryName != null)
                {
                    filteredQuestions = filteredQuestions.Where(q => q.Categories.Contains(categoryName));
                }
            }

            // Apply difficulty filter
            if (!string.IsNullOrWhiteSpace(request.Difficulty))
            {
                filteredQuestions = filteredQuestions.Where(q => q.Difficulty == request.Difficulty);
            }

            var totalCount = filteredQuestions.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

            var pagedQuestions = filteredQuestions
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new QuestionResponse.QuestionBankList
            {
                Questions = pagedQuestions,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalPages = totalPages
            };
        }
    }
}
