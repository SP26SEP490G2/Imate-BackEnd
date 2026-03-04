using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.RequestModels;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.DataAccess.Repositories
{
    public class AccountRepository : RepositoryBase<Account>, IAccountRepository
    {
        public AccountRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }

    public class MentorRepository : RepositoryBase<Mentor>, IMentorRepository
    {
        public MentorRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync()
        {
            return await FindAll(trackChanges: false)
                .Include(m => m.Account)
                .Include(m => m.MentorPositions)
                    .ThenInclude(mp => mp.Position)
                .Include(m => m.MentorCompanies)
                    .ThenInclude(mc => mc.Company)
                .Select(m => new MentorResponse.ListPreviewMentor
                {
                    FullName = m.Account.FullName,
                    Position = m.MentorPositions.FirstOrDefault() != null ? m.MentorPositions.FirstOrDefault().Position.Name : string.Empty,
                    Yoe = m.Yoe,
                    Company = m.MentorCompanies.FirstOrDefault() != null ? m.MentorCompanies.FirstOrDefault().Company.Name : string.Empty,
                    AvgRatings = m.AvgRatings,
                    TotalRatingCount = m.TotalRatingCount
                })
                .ToListAsync();
        }
    }

    public class QuestionRepository : RepositoryBase<Question>, IQuestionRepository
    {
        public QuestionRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
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

    public class CategoryRepository : RepositoryBase<Category>, ICategoryRepository
    {
        public CategoryRepository(ImateDbContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<IEnumerable<QuestionResponse.CategoryItem>> GetListQuestionCategoriesAsync()
        {
            // TODO: Uncomment this when you have real data
            /*
            return await FindAll(trackChanges: false)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .Select(c => new QuestionResponse.CategoryItem
                {
                    Id = c.Id,
                    Name = c.Name
                })
                .ToListAsync();
            */

            // Fake data for testing
            await Task.Delay(50);

            return new List<QuestionResponse.CategoryItem>
            {
                new QuestionResponse.CategoryItem { Id = 1, Name = "Frontend" },
                new QuestionResponse.CategoryItem { Id = 2, Name = "Backend" },
                new QuestionResponse.CategoryItem { Id = 3, Name = "System Design" },
                new QuestionResponse.CategoryItem { Id = 4, Name = "Database" },
                new QuestionResponse.CategoryItem { Id = 5, Name = "DevOps" },
                new QuestionResponse.CategoryItem { Id = 6, Name = "Mobile" },
                new QuestionResponse.CategoryItem { Id = 7, Name = "Security" },
                new QuestionResponse.CategoryItem { Id = 8, Name = "Algorithm" }
            };
        }
    }

    public class UnitOfWork : IUnitOfWork
    {
        private readonly ImateDbContext _repositoryContext;
        private IAccountRepository? _accountRepository;
        private IMentorRepository? _mentorRepository;
        private IQuestionRepository? _questionRepository;
        private ICategoryRepository? _categoryRepository;

        public UnitOfWork(ImateDbContext repositoryContext)
        {
            _repositoryContext = repositoryContext;
        }

        public IAccountRepository Account
        {
            get
            {
                if (_accountRepository == null)
                    _accountRepository = new AccountRepository(_repositoryContext);

                return _accountRepository;
            }
        }

        public IMentorRepository Mentor
        {
            get
            {
                if (_mentorRepository == null)
                    _mentorRepository = new MentorRepository(_repositoryContext);

                return _mentorRepository;
            }
        }

        public IQuestionRepository Question
        {
            get
            {
                if (_questionRepository == null)
                    _questionRepository = new QuestionRepository(_repositoryContext);

                return _questionRepository;
            }
        }

        public ICategoryRepository Category
        {
            get
            {
                if (_categoryRepository == null)
                    _categoryRepository = new CategoryRepository(_repositoryContext);

                return _categoryRepository;
            }
        }

        public Task SaveAsync() => _repositoryContext.SaveChangesAsync();
    }
}
