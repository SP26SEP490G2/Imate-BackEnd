using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.Classification
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ICategoryRepository Object { get; }

        public CategoryService(ICategoryRepository categoryRepository, IQuestionRepository questionRepository, IUnitOfWork unitOfWork)
        {
            _categoryRepository = categoryRepository;
            _questionRepository = questionRepository;
            _unitOfWork = unitOfWork;
        }

        public CategoryService(ICategoryRepository @object)
        {
            Object = @object;
        }

        public async Task<List<int>> GetNonExistingCategoryIdsAsync(IEnumerable<int> categoryIds)
        {
            return await _categoryRepository.GetNonExistingCategoryIdsAsync(categoryIds);
        }
        // PHƯƠNG THỨC MỚI ĐƯỢC CẬP NHẬT LOGIC
        public async Task<PagedList<CategoryResponse>> GetAllCategoryAsync(CommonParams categoryParams)
        {
            var query = _categoryRepository.GetAllCategories();

            // 1. Áp dụng các bộ lọc (Filtering)
            if (!string.IsNullOrWhiteSpace(categoryParams.SearchTerm))
            {
                var searchTerm = categoryParams.SearchTerm.ToLower().Trim();
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
            }

            if (categoryParams.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == categoryParams.IsActive.Value);
            }

            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(categoryParams.SortBy))
            {
                bool isDescending = categoryParams.SortOrder?.ToLower() == "desc";

                query = categoryParams.SortBy.ToLower() switch
                {
                    "name" => isDescending
                        ? query.OrderByDescending(q => q.Name)
                        : query.OrderBy(q => q.Name),

                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),

                    "questioncount" => isDescending
                        ? query.OrderByDescending(q => q.QuestionCategories.Count())
                        : query.OrderBy(q => q.QuestionCategories.Count()),

                    _ => query.OrderByDescending(q => q.CreatedAt)
                };
            }
            else
            {
                // Sắp xếp mặc định (không có yêu cầu sort)
                query = query.OrderByDescending(q => q.CreatedAt);
            }

            // 3. Phân trang trên dữ liệu Category cơ bản.
            // Bước này sẽ chạy CountAsync() trên một query đơn giản -> SẼ KHÔNG CÒN LỖI.
            var pagedCategories = await PagedList<Category>.CreateAsync(query, categoryParams.PageNumber, categoryParams.PageSize);

            // Nếu không có dữ liệu thì trả về ngay
            if (!pagedCategories.Items.Any())
            {
                return new PagedList<CategoryResponse>(new List<CategoryResponse>(), 0, categoryParams.PageNumber, categoryParams.PageSize);
            }

            // 4. Lấy ID của các category chỉ có trong trang này
            var categoryIdsOnPage = pagedCategories.Items.Select(c => c.Id).ToList();

            // 5. Lấy số lượng question cho các category đó bằng MỘT query duy nhất
            var questionCounts = await _questionRepository.GetAllQuestions()
                .SelectMany(q => q.QuestionCategories)
                .Where(qc => categoryIdsOnPage.Contains(qc.CategoryId))
                .GroupBy(qc => qc.CategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            // 6. Ghép dữ liệu lại để tạo ra kết quả cuối cùng
            var responseItems = pagedCategories.Items.Select(c => new CategoryResponse
            {
                Id = c.Id,
                Name = c.Name,
                IsActive = c.IsActive,
                QuestionCount = questionCounts.GetValueOrDefault(c.Id, 0)
            }).ToList();

            // 7. Trả về PagedList<CategoryResponse> hoàn chỉnh
            return new PagedList<CategoryResponse>(responseItems, pagedCategories.TotalCount, pagedCategories.PageNumber, pagedCategories.PageSize);
        }

        public async Task<Category> UpdateCategoriesAsync(int id, CategoryUpdateRequest category)
        {
            // Lấy category từ unit of work
            var existingCategory = await _unitOfWork.Categories.GetCategoryByIdAsync(id);
            if (existingCategory == null)
            {
                throw new NotFoundException($"Category with Id {id} not found");
            }

            existingCategory.Name = category.Name;
            existingCategory.IsActive = category.IsActive;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            if (category.IsActive == false)
            {
                // Lấy questions từ unit of work
                var questionsToUpdate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == id) && q.IsActive)
                                             .ToListAsync();

                foreach (var q in questionsToUpdate)
                {
                    q.UpdatedAt = DateTime.UtcNow;
                    q.IsActive = false;
                }
            }

            else if (category.IsActive == true)
            {
                // SỬA LỖI: Lọc theo QuestionCategories
                // Mục tiêu là kích hoạt các câu hỏi liên quan đến Category này đang bị inactive
                var questionsToReActivate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                    .Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == id) && !q.IsActive
                    && q.UpdatedAt != null) // Tìm câu hỏi đang inactive (!q.IsActive)
                    .ToListAsync();

                foreach (var question in questionsToReActivate)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = true;
                }
            }

            // 🔑 GỌI SAVECHANGES MỘT LẦN DUY NHẤT TỪ UNIT OF WORK
            // Nó sẽ lưu tất cả các thay đổi của cả Category và Question trong một transaction.
            await _unitOfWork.SaveChangesAsync();

            return existingCategory;
        }
        public async Task<Category> AddCategoriesAsync(CategoryCreateRequest category)
        {
            var newCategory = new Category
            {
                Name = category.Name,
                CreatedAt = DateTime.UtcNow,
                IsActive = true

            };
            await _categoryRepository.AddCategory(newCategory);
            return newCategory;
        }
        //Test đến đây rồi
        public async Task<List<AffectedQuestionResponseModel>> GetAffectedQuestionsAsync(int categoryId, bool willBeActive)
        {
            if (willBeActive)
            {
                // Trả về danh sách rỗng vì kích hoạt không ảnh hưởng tiêu cực
                return new List<AffectedQuestionResponseModel>();
            }

            // Lấy các câu hỏi sẽ bị ẩn
            var affectedQuestions = await _unitOfWork.Questions.GetAllQuestions()
                .Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == categoryId) && q.IsActive && q.UpdatedAt == null)
                .Select(q => new AffectedQuestionResponseModel
                {
                    Id = q.Id,
                    Content = q.Content,
                    DifficultyLevel = q.Difficulty.HasValue ? q.Difficulty.Value.ToString() : null
                })
                .ToListAsync();

            return affectedQuestions;
        }

        public async Task<Category?> SetCategoryStatusAsync(int id, bool isActive)
        {
            var existingCategory = await _unitOfWork.Categories.GetCategoryByIdAsync(id);
            if (existingCategory == null)
            {
                return null;
            }

            existingCategory.IsActive = isActive;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            // Logic tương tự UpdateCategoriesAsync để cập nhật questions liên quan
            if (isActive == false)
            {
                var questionsToUpdate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == id) && q.IsActive)
                                             .ToListAsync();

                foreach (var q in questionsToUpdate)
                {
                    q.UpdatedAt = DateTime.UtcNow;
                    q.IsActive = false;
                }
            }
            else if (isActive == true)
            {
                var questionsToReActivate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                    .Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == id) && !q.IsActive
                    && q.UpdatedAt != null)
                    .ToListAsync();

                foreach (var question in questionsToReActivate)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = true;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return existingCategory;
        }
    }
}
