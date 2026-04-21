using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels.QuestionBank;
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
            _context = repositoryContext;
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
                .Where(q => q.Id == questionId)
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
                .ToListAsync();
        }

        public async Task CreateContributedQuestionAsync(Question question)
        {
            try
            {
                await _context.Questions.AddAsync(question);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
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

        public async Task<Question> GetQuestionByIdAsync(int questionId, bool isFromSystem)
        {
            var a = await _context.Questions
                .Where(q => q.Id == questionId)
                .Include(q => q.Creator)
                .Include(q => q.ContributedDetail)
                    .ThenInclude(cd => cd.Company)
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionPositions)
                    .ThenInclude(qp => qp.Position)
                .Include(q => q.Comments)
                    .ThenInclude(c => c.User)
                        .ThenInclude(c => c.AccountRoles)
                            .ThenInclude(c => c.Role)
                .Include(q => q.Comments)
                    .ThenInclude(c => c.Votes)
                    .Where(q => q.IsFromSystem == isFromSystem)
                          .FirstOrDefaultAsync();
            return a;
        }

        public async Task<Question> GetQuestionByIdAsync(int questionId)
        {
            var a = await _context.Questions
                .Where(q => q.Id == questionId)
                .Include(q => q.Creator)
                .Include(q => q.ContributedDetail)
                    .ThenInclude(cd => cd.Company)
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
            // Bỏ qua các câu hỏi đã bị Rejected, vì câu bị từ chối không nên cản trở việc tạo câu hỏi mới
            var existingContents = await _context.Questions
                .Where(q => contents.Contains(q.Content) && q.ApprovalStatus != QuestionApprovalStatus.Rejected)
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
                .Take(5)
                .Select(q => new QuestionResponse.ListHotQuestion
                {
                    Id = q.Id,
                    Content = q.Content,
                    Categories = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                    CommentCount = q.Comments.Count
                })
                .ToListAsync();
        }

        public IQueryable<Question> GetQuestionBankListAsync()
        {
            return _context.Questions
                .Include(q => q.QuestionCategories)
                    .ThenInclude(qc => qc.Category)
                .Include(q => q.QuestionSkills)
                    .ThenInclude(qs => qs.Skill)
                .Include(q => q.Comments)
                .Include(q => q.Creator)
                .Where(q => q.IsActive).AsNoTracking();
        }

        public async Task<IEnumerable<QuestionResponse.QuestionCategoryItem>> GetListQuestionCategoriesAsync()
        {
            return await _context.QuestionCategories
                .Include(qc => qc.Category)
                .Select(c => new QuestionResponse.QuestionCategoryItem
                {
                    Id = c.CategoryId,
                    Name = c.Category.Name
                })
                .ToListAsync();
        }
    }
}
