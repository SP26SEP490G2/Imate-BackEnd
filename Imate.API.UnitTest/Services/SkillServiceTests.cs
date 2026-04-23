using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Classification;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using Microsoft.EntityFrameworkCore;
using MockQueryable;
using MockQueryable.Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class SkillServiceTests
    {
        private readonly Mock<ISkillRepository> _mockSkillRepo;
        private readonly Mock<IQuestionRepository> _mockQuestionRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly ImateDbContext _dbContext;
        private readonly SkillService _service;

        public SkillServiceTests()
        {
            _mockSkillRepo = new Mock<ISkillRepository>();
            _mockQuestionRepo = new Mock<IQuestionRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            var options = new DbContextOptionsBuilder<ImateDbContext>()
                .UseInMemoryDatabase(databaseName: $"SkillTestDb_{Guid.NewGuid()}")
                .Options;
            _dbContext = new ImateDbContext(options);

            _mockUnitOfWork.Setup(u => u.Questions).Returns(_mockQuestionRepo.Object);

            _service = new SkillService(
                _mockSkillRepo.Object,
                _mockQuestionRepo.Object,
                _mockUnitOfWork.Object,
                _dbContext);
        }

        #region AddSkillsAsync

        // Kiểm tra tạo skill hợp lệ: Name đúng, IsActive = true, CreatedAt gần thời điểm hiện tại, repository được gọi 1 lần
        [Fact]
        public async Task AddSkillsAsync_ShouldCreateSkill_WhenRequestIsValid()
        {
            var request = new SkillCreateRequest { Name = "C#" };
            _mockSkillRepo
                .Setup(r => r.AddSkillAsync(It.IsAny<Skill>()))
                .ReturnsAsync((Skill s) => s);

            var result = await _service.AddSkillsAsync(request);

            result.Should().NotBeNull();
            result.Name.Should().Be("C#");
            result.IsActive.Should().BeTrue();
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            _mockSkillRepo.Verify(r => r.AddSkillAsync(It.Is<Skill>(s => s.Name == "C#" && s.IsActive)), Times.Once);
        }

        // Kiểm tra skill mới tạo luôn có IsActive = true mặc định
        [Fact]
        public async Task AddSkillsAsync_ShouldSetIsActiveTrue_ByDefault()
        {
            var request = new SkillCreateRequest { Name = "React" };
            _mockSkillRepo
                .Setup(r => r.AddSkillAsync(It.IsAny<Skill>()))
                .ReturnsAsync((Skill s) => s);

            var result = await _service.AddSkillsAsync(request);

            result.IsActive.Should().BeTrue();
        }

        #endregion

        #region UpdateSkillsAsync

        // Kiểm tra cập nhật Name thành công khi skill tồn tại, UpdatedAt được gán, SaveChanges gọi 1 lần
        [Fact]
        public async Task UpdateSkillsAsync_ShouldUpdateNameAndStatus_WhenSkillExists()
        {
            var skillId = 1;
            var existingSkill = new Skill { Id = skillId, Name = "Old C#", IsActive = true };
            var request = new SkillUpdateRequest { Name = "C# .NET", IsActive = true };

            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(skillId)).ReturnsAsync(existingSkill);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(emptyQuestions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdateSkillsAsync(skillId, request);

            result.Name.Should().Be("C# .NET");
            result.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra ném NotFoundException khi skill ID không tồn tại
        [Fact]
        public async Task UpdateSkillsAsync_ShouldThrowNotFoundException_WhenSkillNotFound()
        {
            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(It.IsAny<int>())).ReturnsAsync((Skill?)null);
            var request = new SkillUpdateRequest { Name = "Test", IsActive = true };

            var act = () => _service.UpdateSkillsAsync(999, request);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*999*");
        }

        // Kiểm tra cascade deactivate: khi skill bị tắt → question liên kết qua QuestionSkills cũng bị tắt
        [Fact]
        public async Task UpdateSkillsAsync_ShouldDeactivateRelatedQuestions_WhenSkillIsDeactivated()
        {
            var skillId = 1;
            var existingSkill = new Skill { Id = skillId, Name = "C#", IsActive = true };
            var request = new SkillUpdateRequest { Name = "C#", IsActive = false };

            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(skillId)).ReturnsAsync(existingSkill);

            var relatedQuestion = new Question
            {
                Id = 10, IsActive = true,
                QuestionSkills = new List<QuestionSkill> { new QuestionSkill { SkillId = skillId } }
            };

            var questions = new List<Question> { relatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdateSkillsAsync(skillId, request);

            relatedQuestion.IsActive.Should().BeFalse();
            relatedQuestion.UpdatedAt.Should().NotBeNull();
        }

        // Kiểm tra cascade reactivate: khi skill bật lại → question có UpdatedAt != null được bật lại
        [Fact]
        public async Task UpdateSkillsAsync_ShouldReactivateRelatedQuestions_WhenSkillIsActivated()
        {
            var skillId = 1;
            var existingSkill = new Skill { Id = skillId, Name = "C#", IsActive = false };
            var request = new SkillUpdateRequest { Name = "C#", IsActive = true };

            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(skillId)).ReturnsAsync(existingSkill);

            var deactivatedQuestion = new Question
            {
                Id = 10, IsActive = false, UpdatedAt = DateTime.UtcNow.AddDays(-1),
                QuestionSkills = new List<QuestionSkill> { new QuestionSkill { SkillId = skillId } }
            };

            var questions = new List<Question> { deactivatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdateSkillsAsync(skillId, request);

            deactivatedQuestion.IsActive.Should().BeTrue();
        }

        // Kiểm tra edge case: question có UpdatedAt = null (bị tắt thủ công) KHÔNG được bật lại khi cascade reactivate
        [Fact]
        public async Task UpdateSkillsAsync_ShouldNotReactivateQuestions_WithNullUpdatedAt()
        {
            var skillId = 1;
            var existingSkill = new Skill { Id = skillId, Name = "C#", IsActive = false };
            var request = new SkillUpdateRequest { Name = "C#", IsActive = true };

            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(skillId)).ReturnsAsync(existingSkill);

            var questionNullUpdated = new Question
            {
                Id = 10, IsActive = false, UpdatedAt = null,
                QuestionSkills = new List<QuestionSkill> { new QuestionSkill { SkillId = skillId } }
            };

            var questions = new List<Question> { questionNullUpdated }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdateSkillsAsync(skillId, request);

            questionNullUpdated.IsActive.Should().BeFalse();
        }

        #endregion

        #region SetSkillStatusAsync

        // Kiểm tra trả null khi skill ID không tồn tại
        [Fact]
        public async Task SetSkillStatusAsync_ShouldReturnNull_WhenSkillNotFound()
        {
            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(It.IsAny<int>())).ReturnsAsync((Skill?)null);

            var result = await _service.SetSkillStatusAsync(999, true);

            result.Should().BeNull();
        }

        // Kiểm tra bật skill: IsActive = true, UpdatedAt được gán, SaveChanges gọi 1 lần
        [Fact]
        public async Task SetSkillStatusAsync_ShouldActivateSkill()
        {
            var skill = new Skill { Id = 1, Name = "C#", IsActive = false };
            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(1)).ReturnsAsync(skill);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(emptyQuestions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SetSkillStatusAsync(1, true);

            result!.IsActive.Should().BeTrue();
            result.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra tắt skill: IsActive = false
        [Fact]
        public async Task SetSkillStatusAsync_ShouldDeactivateSkill()
        {
            var skill = new Skill { Id = 1, Name = "C#", IsActive = true };
            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(1)).ReturnsAsync(skill);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(emptyQuestions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SetSkillStatusAsync(1, false);

            result!.IsActive.Should().BeFalse();
        }

        // Kiểm tra cascade deactivate question khi tắt skill qua SetStatus
        [Fact]
        public async Task SetSkillStatusAsync_ShouldCascadeDeactivateQuestions()
        {
            var skill = new Skill { Id = 1, IsActive = true };
            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(1)).ReturnsAsync(skill);

            var activeQuestion = new Question
            {
                Id = 10, IsActive = true,
                QuestionSkills = new List<QuestionSkill> { new QuestionSkill { SkillId = 1 } }
            };

            var questions = new List<Question> { activeQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.SetSkillStatusAsync(1, false);

            activeQuestion.IsActive.Should().BeFalse();
        }

        // Kiểm tra cascade reactivate question (có UpdatedAt != null) khi bật skill qua SetStatus
        [Fact]
        public async Task SetSkillStatusAsync_ShouldCascadeReactivateQuestions()
        {
            var skill = new Skill { Id = 1, IsActive = false };
            _mockSkillRepo.Setup(r => r.GetSkillByIdAsync(1)).ReturnsAsync(skill);

            var deactivatedQuestion = new Question
            {
                Id = 10, IsActive = false, UpdatedAt = DateTime.UtcNow.AddDays(-1),
                QuestionSkills = new List<QuestionSkill> { new QuestionSkill { SkillId = 1 } }
            };

            var questions = new List<Question> { deactivatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.SetSkillStatusAsync(1, true);

            deactivatedQuestion.IsActive.Should().BeTrue();
        }

        #endregion

        #region GetAffectedQuestionsAsync

        // Kiểm tra trả list rỗng khi bật skill (không ảnh hưởng tiêu cực)
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldReturnEmptyList_WhenWillBeActiveIsTrue()
        {
            var result = await _service.GetAffectedQuestionsAsync(1, willBeActive: true);

            result.Should().BeEmpty();
        }

        // Kiểm tra trả danh sách question active sẽ bị ảnh hưởng khi tắt skill
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldReturnAffectedQuestions_WhenDeactivating()
        {
            var question = new Question
            {
                Id = 10, Content = "What is C#?", IsActive = true,
                QuestionSkills = new List<QuestionSkill> { new QuestionSkill { SkillId = 1 } }
            };

            var allQuestions = new List<Question> { question }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(allQuestions);

            var result = await _service.GetAffectedQuestionsAsync(1, willBeActive: false);

            result.Should().HaveCount(1);
            result[0].Content.Should().Be("What is C#?");
        }

        // Kiểm tra question đã inactive không nằm trong danh sách bị ảnh hưởng
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldExcludeInactiveQuestions()
        {
            var inactiveQuestion = new Question
            {
                Id = 10, IsActive = false,
                QuestionSkills = new List<QuestionSkill> { new QuestionSkill { SkillId = 1 } }
            };

            var allQuestions = new List<Question> { inactiveQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(allQuestions);

            var result = await _service.GetAffectedQuestionsAsync(1, willBeActive: false);

            result.Should().BeEmpty();
        }

        #endregion

        #region GetNonExistingSkillIdsAsync

        // Kiểm tra trả về danh sách ID không tồn tại: input [1,2,99] → trả [99]
        [Fact]
        public async Task GetNonExistingSkillIdsAsync_ShouldReturnNonExistingIds()
        {
            var inputIds = new List<int> { 1, 2, 99 };
            _mockSkillRepo.Setup(r => r.GetNonExistingSkillIdsAsync(inputIds)).ReturnsAsync(new List<int> { 99 });

            var result = await _service.GetNonExistingSkillIdsAsync(inputIds);

            result.Should().HaveCount(1);
            result.Should().Contain(99);
        }

        // Kiểm tra trả list rỗng khi tất cả ID đều tồn tại
        [Fact]
        public async Task GetNonExistingSkillIdsAsync_ShouldReturnEmptyList_WhenAllExist()
        {
            var inputIds = new List<int> { 1, 2 };
            _mockSkillRepo.Setup(r => r.GetNonExistingSkillIdsAsync(inputIds)).ReturnsAsync(new List<int>());

            var result = await _service.GetNonExistingSkillIdsAsync(inputIds);

            result.Should().BeEmpty();
        }

        #endregion
    }
}
