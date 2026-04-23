using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Classification;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class CategoryServiceTests
    {
        private readonly Mock<ICategoryRepository> _mockCategoryRepo;
        private readonly Mock<IQuestionRepository> _mockQuestionRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _mockCategoryRepo = new Mock<ICategoryRepository>();
            _mockQuestionRepo = new Mock<IQuestionRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _mockUnitOfWork.Setup(u => u.Categories).Returns(_mockCategoryRepo.Object);
            _mockUnitOfWork.Setup(u => u.Questions).Returns(_mockQuestionRepo.Object);

            _service = new CategoryService(
                _mockCategoryRepo.Object,
                _mockQuestionRepo.Object,
                _mockUnitOfWork.Object);
        }

        #region AddCategoriesAsync

        // Kiểm tra tạo category hợp lệ: Name đúng, IsActive = true, CreatedAt gần thời điểm hiện tại, repository được gọi 1 lần
        [Fact]
        public async Task AddCategoriesAsync_ShouldCreateCategory_WhenRequestIsValid()
        {
            var request = new CategoryCreateRequest { Name = "Backend" };

            _mockCategoryRepo
                .Setup(r => r.AddCategory(It.IsAny<Category>()))
                .Returns(Task.FromResult(new Category { Id = 1, Name = "Backend", IsActive = true }));

            var result = await _service.AddCategoriesAsync(request);

            result.Should().NotBeNull();
            result.Name.Should().Be("Backend");
            result.IsActive.Should().BeTrue();
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _mockCategoryRepo.Verify(r => r.AddCategory(It.Is<Category>(c => c.Name == "Backend" && c.IsActive)), Times.Once);
        }

        // Kiểm tra category mới tạo luôn có IsActive = true mặc định
        [Fact]
        public async Task AddCategoriesAsync_ShouldSetIsActiveTrue_ByDefault()
        {
            var request = new CategoryCreateRequest { Name = "Frontend" };

            _mockCategoryRepo
                .Setup(r => r.AddCategory(It.IsAny<Category>()))
                .Returns(Task.FromResult(new Category()));

            var result = await _service.AddCategoriesAsync(request);

            result.IsActive.Should().BeTrue();
        }

        #endregion

        #region UpdateCategoriesAsync

        // Kiểm tra cập nhật Name và trạng thái thành công khi category tồn tại, UpdatedAt được gán, SaveChanges gọi 1 lần
        [Fact]
        public async Task UpdateCategoriesAsync_ShouldUpdateNameAndStatus_WhenCategoryExists()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Old Name", IsActive = true };
            var request = new CategoryUpdateRequest { Name = "New Name", IsActive = true };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(emptyQuestions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateCategoriesAsync(categoryId, request);

            result.Should().NotBeNull();
            result.Name.Should().Be("New Name");
            result.IsActive.Should().BeTrue();
            result.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra ném NotFoundException khi category ID không tồn tại
        [Fact]
        public async Task UpdateCategoriesAsync_ShouldThrowNotFoundException_WhenCategoryNotFound()
        {
            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Category?)null);

            var request = new CategoryUpdateRequest { Name = "Test", IsActive = true };

            var act = () => _service.UpdateCategoriesAsync(999, request);

            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("*999*");
        }

        // Kiểm tra cascade deactivate: khi category bị tắt → các question liên kết cũng bị tắt và UpdatedAt được cập nhật
        [Fact]
        public async Task UpdateCategoriesAsync_ShouldDeactivateRelatedQuestions_WhenCategoryIsDeactivated()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Backend", IsActive = true };
            var request = new CategoryUpdateRequest { Name = "Backend", IsActive = false };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var relatedQuestion = new Question
            {
                Id = 10,
                Content = "What is REST?",
                IsActive = true,
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var questions = new List<Question> { relatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(questions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateCategoriesAsync(categoryId, request);

            result.IsActive.Should().BeFalse();
            relatedQuestion.IsActive.Should().BeFalse();
            relatedQuestion.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra cascade reactivate: khi category bật lại → question có UpdatedAt != null được bật lại
        [Fact]
        public async Task UpdateCategoriesAsync_ShouldReactivateRelatedQuestions_WhenCategoryIsActivated()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Backend", IsActive = false };
            var request = new CategoryUpdateRequest { Name = "Backend", IsActive = true };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var deactivatedQuestion = new Question
            {
                Id = 10,
                Content = "What is REST?",
                IsActive = false,
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var questions = new List<Question> { deactivatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(questions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateCategoriesAsync(categoryId, request);

            result.IsActive.Should().BeTrue();
            deactivatedQuestion.IsActive.Should().BeTrue();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra edge case: question có UpdatedAt = null (bị tắt thủ công) KHÔNG được bật lại khi cascade reactivate
        [Fact]
        public async Task UpdateCategoriesAsync_ShouldNotReactivateQuestions_WithNullUpdatedAt()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Backend", IsActive = false };
            var request = new CategoryUpdateRequest { Name = "Backend", IsActive = true };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var questionWithNullUpdatedAt = new Question
            {
                Id = 10,
                Content = "What is REST?",
                IsActive = false,
                UpdatedAt = null,
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var questions = new List<Question> { questionWithNullUpdatedAt }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(questions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdateCategoriesAsync(categoryId, request);

            questionWithNullUpdatedAt.IsActive.Should().BeFalse();
        }

        #endregion

        #region SetCategoryStatusAsync

        // Kiểm tra trả null khi category ID không tồn tại
        [Fact]
        public async Task SetCategoryStatusAsync_ShouldReturnNull_WhenCategoryNotFound()
        {
            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Category?)null);

            var result = await _service.SetCategoryStatusAsync(999, true);

            result.Should().BeNull();
        }

        // Kiểm tra bật category: IsActive = true, UpdatedAt được gán, SaveChanges gọi 1 lần
        [Fact]
        public async Task SetCategoryStatusAsync_ShouldActivateCategory_WhenCalledWithTrue()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Backend", IsActive = false };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var questions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(questions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SetCategoryStatusAsync(categoryId, true);

            result.Should().NotBeNull();
            result!.IsActive.Should().BeTrue();
            result.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra tắt category: IsActive = false
        [Fact]
        public async Task SetCategoryStatusAsync_ShouldDeactivateCategory_WhenCalledWithFalse()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Backend", IsActive = true };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var questions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(questions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SetCategoryStatusAsync(categoryId, false);

            result.Should().NotBeNull();
            result!.IsActive.Should().BeFalse();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra cascade deactivate question khi tắt category qua SetStatus
        [Fact]
        public async Task SetCategoryStatusAsync_ShouldDeactivateRelatedQuestions_WhenDeactivating()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Backend", IsActive = true };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var activeQuestion = new Question
            {
                Id = 10,
                IsActive = true,
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var questions = new List<Question> { activeQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(questions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.SetCategoryStatusAsync(categoryId, false);

            activeQuestion.IsActive.Should().BeFalse();
            activeQuestion.UpdatedAt.Should().NotBeNull();
        }

        // Kiểm tra cascade reactivate question (có UpdatedAt != null) khi bật category qua SetStatus
        [Fact]
        public async Task SetCategoryStatusAsync_ShouldReactivateRelatedQuestions_WhenActivating()
        {
            var categoryId = 1;
            var existingCategory = new Category { Id = categoryId, Name = "Backend", IsActive = false };

            _mockCategoryRepo
                .Setup(r => r.GetCategoryByIdAsync(categoryId))
                .ReturnsAsync(existingCategory);

            var deactivatedQuestion = new Question
            {
                Id = 10,
                IsActive = false,
                UpdatedAt = DateTime.UtcNow.AddDays(-1),
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var questions = new List<Question> { deactivatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestionsTracking())
                .Returns(questions);

            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.SetCategoryStatusAsync(categoryId, true);

            deactivatedQuestion.IsActive.Should().BeTrue();
        }

        #endregion

        #region GetAffectedQuestionsAsync

        // Kiểm tra trả list rỗng khi bật category (không ảnh hưởng tiêu cực)
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldReturnEmptyList_WhenWillBeActiveIsTrue()
        {
            var categoryId = 1;

            var result = await _service.GetAffectedQuestionsAsync(categoryId, willBeActive: true);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        // Kiểm tra trả danh sách question active sẽ bị ảnh hưởng khi tắt category
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldReturnAffectedQuestions_WhenWillBeActiveIsFalse()
        {
            var categoryId = 1;

            var affectedQuestion = new Question
            {
                Id = 10,
                Content = "What is REST API?",
                Difficulty = Models.Enums.DifficultyLevel.Hard,
                IsActive = true,
                UpdatedAt = null,
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var allQuestions = new List<Question> { affectedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestions())
                .Returns(allQuestions);

            var result = await _service.GetAffectedQuestionsAsync(categoryId, willBeActive: false);

            result.Should().HaveCount(1);
            result[0].Id.Should().Be(10);
            result[0].Content.Should().Be("What is REST API?");
        }

        // Kiểm tra question đã inactive không nằm trong danh sách bị ảnh hưởng
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldExcludeInactiveQuestions_WhenWillBeActiveIsFalse()
        {
            var categoryId = 1;

            var inactiveQuestion = new Question
            {
                Id = 10,
                Content = "Old question",
                IsActive = false,
                UpdatedAt = null,
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var allQuestions = new List<Question> { inactiveQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestions())
                .Returns(allQuestions);

            var result = await _service.GetAffectedQuestionsAsync(categoryId, willBeActive: false);

            result.Should().BeEmpty();
        }

        // Kiểm tra question có UpdatedAt != null bị loại khỏi danh sách affected
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldExcludeQuestions_WithNonNullUpdatedAt()
        {
            var categoryId = 1;

            var modifiedQuestion = new Question
            {
                Id = 10,
                Content = "Modified question",
                IsActive = true,
                UpdatedAt = DateTime.UtcNow,
                QuestionCategories = new List<QuestionCategory>
                {
                    new QuestionCategory { CategoryId = categoryId }
                }
            };

            var allQuestions = new List<Question> { modifiedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo
                .Setup(r => r.GetAllQuestions())
                .Returns(allQuestions);

            var result = await _service.GetAffectedQuestionsAsync(categoryId, willBeActive: false);

            result.Should().BeEmpty();
        }

        #endregion

        #region GetNonExistingCategoryIdsAsync

        // Kiểm tra trả về danh sách ID không tồn tại trong database
        [Fact]
        public async Task GetNonExistingCategoryIdsAsync_ShouldReturnNonExistingIds()
        {
            var inputIds = new List<int> { 1, 2, 3, 99, 100 };
            var expectedNonExisting = new List<int> { 99, 100 };

            _mockCategoryRepo
                .Setup(r => r.GetNonExistingCategoryIdsAsync(inputIds))
                .ReturnsAsync(expectedNonExisting);

            var result = await _service.GetNonExistingCategoryIdsAsync(inputIds);

            result.Should().HaveCount(2);
            result.Should().Contain(99);
            result.Should().Contain(100);
        }

        // Kiểm tra trả list rỗng khi tất cả ID đều tồn tại
        [Fact]
        public async Task GetNonExistingCategoryIdsAsync_ShouldReturnEmptyList_WhenAllExist()
        {
            var inputIds = new List<int> { 1, 2, 3 };

            _mockCategoryRepo
                .Setup(r => r.GetNonExistingCategoryIdsAsync(inputIds))
                .ReturnsAsync(new List<int>());

            var result = await _service.GetNonExistingCategoryIdsAsync(inputIds);

            result.Should().BeEmpty();
        }

        #endregion

        #region GetAllCategoryAsync

        // Kiểm tra trả về danh sách phân trang mặc định (page 1, size 10) với đầy đủ items
        [Fact]
        public async Task GetAllCategoryAsync_ShouldReturnPagedCategories_WithDefaultPagination()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Backend", IsActive = true, CreatedAt = DateTimeOffset.UtcNow.AddDays(-2), QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 2, Name = "Frontend", IsActive = true, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1), QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 3, Name = "DevOps", IsActive = false, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
            };

            var mockQueryable = categories.AsQueryable().BuildMock();
            _mockCategoryRepo.Setup(r => r.GetAllCategories()).Returns(mockQueryable);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(emptyQuestions);

            var commonParams = new CommonParams { PageNumber = 1, PageSize = 10 };

            var result = await _service.GetAllCategoryAsync(commonParams);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(3);
            result.TotalCount.Should().Be(3);
        }

        // Kiểm tra lọc theo SearchTerm: "end" → match "Backend" và "Frontend"
        [Fact]
        public async Task GetAllCategoryAsync_ShouldFilterBySearchTerm()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Backend", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 2, Name = "Frontend", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 3, Name = "DevOps", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
            };

            var mockQueryable = categories.AsQueryable().BuildMock();
            _mockCategoryRepo.Setup(r => r.GetAllCategories()).Returns(mockQueryable);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(emptyQuestions);

            var commonParams = new CommonParams { PageNumber = 1, PageSize = 10, SearchTerm = "end" };

            var result = await _service.GetAllCategoryAsync(commonParams);

            result.Items.Should().HaveCount(2);
            result.Items.Should().Contain(c => c.Name == "Backend");
            result.Items.Should().Contain(c => c.Name == "Frontend");
        }

        // Kiểm tra lọc theo IsActive = true → chỉ trả category đang active
        [Fact]
        public async Task GetAllCategoryAsync_ShouldFilterByIsActive()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Backend", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 2, Name = "Frontend", IsActive = false, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
            };

            var mockQueryable = categories.AsQueryable().BuildMock();
            _mockCategoryRepo.Setup(r => r.GetAllCategories()).Returns(mockQueryable);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(emptyQuestions);

            var commonParams = new CommonParams { PageNumber = 1, PageSize = 10, IsActive = true };

            var result = await _service.GetAllCategoryAsync(commonParams);

            result.Items.Should().HaveCount(1);
            result.Items[0].Name.Should().Be("Backend");
        }

        // Kiểm tra trả list rỗng khi không có category nào trong database
        [Fact]
        public async Task GetAllCategoryAsync_ShouldReturnEmptyPagedList_WhenNoCategories()
        {
            var categories = new List<Category>().AsQueryable().BuildMock();
            _mockCategoryRepo.Setup(r => r.GetAllCategories()).Returns(categories);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(emptyQuestions);

            var commonParams = new CommonParams { PageNumber = 1, PageSize = 10 };

            var result = await _service.GetAllCategoryAsync(commonParams);

            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
        }

        // Kiểm tra sắp xếp theo Name tăng dần: Alpha → Middle → Zebra
        [Fact]
        public async Task GetAllCategoryAsync_ShouldSortByNameAsc()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Zebra", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 2, Name = "Alpha", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 3, Name = "Middle", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
            };

            var mockQueryable = categories.AsQueryable().BuildMock();
            _mockCategoryRepo.Setup(r => r.GetAllCategories()).Returns(mockQueryable);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(emptyQuestions);

            var commonParams = new CommonParams { PageNumber = 1, PageSize = 10, SortBy = "name", SortOrder = "asc" };

            var result = await _service.GetAllCategoryAsync(commonParams);

            result.Items[0].Name.Should().Be("Alpha");
            result.Items[1].Name.Should().Be("Middle");
            result.Items[2].Name.Should().Be("Zebra");
        }

        // Kiểm tra sắp xếp theo Name giảm dần: Zebra → Alpha
        [Fact]
        public async Task GetAllCategoryAsync_ShouldSortByNameDesc()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Alpha", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
                new Category { Id = 2, Name = "Zebra", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
            };

            var mockQueryable = categories.AsQueryable().BuildMock();
            _mockCategoryRepo.Setup(r => r.GetAllCategories()).Returns(mockQueryable);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(emptyQuestions);

            var commonParams = new CommonParams { PageNumber = 1, PageSize = 10, SortBy = "name", SortOrder = "desc" };

            var result = await _service.GetAllCategoryAsync(commonParams);

            result.Items[0].Name.Should().Be("Zebra");
            result.Items[1].Name.Should().Be("Alpha");
        }

        // Kiểm tra QuestionCount được tính đúng: 3 question liên kết → QuestionCount = 3
        [Fact]
        public async Task GetAllCategoryAsync_ShouldIncludeQuestionCount()
        {
            var categories = new List<Category>
            {
                new Category { Id = 1, Name = "Backend", IsActive = true, CreatedAt = DateTimeOffset.UtcNow, QuestionCategories = new List<QuestionCategory>() },
            };

            var mockQueryable = categories.AsQueryable().BuildMock();
            _mockCategoryRepo.Setup(r => r.GetAllCategories()).Returns(mockQueryable);

            var questions = new List<Question>
            {
                new Question { Id = 10, QuestionCategories = new List<QuestionCategory> { new QuestionCategory { CategoryId = 1 } } },
                new Question { Id = 11, QuestionCategories = new List<QuestionCategory> { new QuestionCategory { CategoryId = 1 } } },
                new Question { Id = 12, QuestionCategories = new List<QuestionCategory> { new QuestionCategory { CategoryId = 1 } } },
            }.AsQueryable().BuildMock();

            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(questions);

            var commonParams = new CommonParams { PageNumber = 1, PageSize = 10 };

            var result = await _service.GetAllCategoryAsync(commonParams);

            result.Items.Should().HaveCount(1);
            result.Items[0].QuestionCount.Should().Be(3);
        }

        #endregion
    }
}
