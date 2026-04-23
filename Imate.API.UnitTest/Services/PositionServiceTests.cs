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
    public class PositionServiceTests
    {
        private readonly Mock<IPositionRepository> _mockPositionRepo;
        private readonly Mock<ISkillRepository> _mockSkillRepo;
        private readonly Mock<IQuestionRepository> _mockQuestionRepo;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly PositionService _service;

        public PositionServiceTests()
        {
            _mockPositionRepo = new Mock<IPositionRepository>();
            _mockSkillRepo = new Mock<ISkillRepository>();
            _mockQuestionRepo = new Mock<IQuestionRepository>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();

            _mockUnitOfWork.Setup(u => u.Questions).Returns(_mockQuestionRepo.Object);

            _service = new PositionService(
                _mockPositionRepo.Object,
                _mockSkillRepo.Object,
                _mockQuestionRepo.Object,
                _mockUnitOfWork.Object);
        }

        #region AddPositionAsync

        // Kiểm tra tạo position hợp lệ: Name đúng, IsActive = true, CreatedAt gần thời điểm hiện tại, repository được gọi 1 lần
        [Fact]
        public async Task AddPositionAsync_ShouldCreatePosition_WhenRequestIsValid()
        {
            var request = new PositionCreateRequest { Name = "Backend Developer" };
            _mockPositionRepo
                .Setup(r => r.AddPositionAsync(It.IsAny<Position>()))
                .ReturnsAsync((Position p) => p);

            var result = await _service.AddPositionAsync(request);

            result.Should().NotBeNull();
            result.Name.Should().Be("Backend Developer");
            result.IsActive.Should().BeTrue();
            result.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
            _mockPositionRepo.Verify(r => r.AddPositionAsync(It.Is<Position>(p => p.Name == "Backend Developer" && p.IsActive)), Times.Once);
        }

        // Kiểm tra position mới tạo luôn có IsActive = true mặc định
        [Fact]
        public async Task AddPositionAsync_ShouldSetIsActiveTrue_ByDefault()
        {
            var request = new PositionCreateRequest { Name = "QA Engineer" };
            _mockPositionRepo
                .Setup(r => r.AddPositionAsync(It.IsAny<Position>()))
                .ReturnsAsync((Position p) => p);

            var result = await _service.AddPositionAsync(request);

            result.IsActive.Should().BeTrue();
        }

        #endregion

        #region UpdatePositionAsync

        // Kiểm tra cập nhật Name thành công khi position tồn tại, UpdatedAt được gán, SaveChanges gọi 1 lần
        [Fact]
        public async Task UpdatePositionAsync_ShouldUpdateNameAndStatus_WhenPositionExists()
        {
            var positionId = 1;
            var existingPosition = new Position { Id = positionId, Name = "Old Name", IsActive = true };
            var request = new PositionUpdateRequest { Name = "New Name", IsActive = true };

            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(positionId)).ReturnsAsync(existingPosition);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(emptyQuestions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.UpdatePositionAsync(positionId, request);

            result.Name.Should().Be("New Name");
            result.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra ném NotFoundException khi position ID không tồn tại
        [Fact]
        public async Task UpdatePositionAsync_ShouldThrowNotFoundException_WhenPositionNotFound()
        {
            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(It.IsAny<int>())).ReturnsAsync((Position?)null);
            var request = new PositionUpdateRequest { Name = "Test", IsActive = true };

            var act = () => _service.UpdatePositionAsync(999, request);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*999*");
        }

        // Kiểm tra cascade deactivate: khi position bị tắt → question liên kết qua QuestionPositions cũng bị tắt
        [Fact]
        public async Task UpdatePositionAsync_ShouldDeactivateRelatedQuestions_WhenPositionIsDeactivated()
        {
            var positionId = 1;
            var existingPosition = new Position { Id = positionId, Name = "Backend", IsActive = true };
            var request = new PositionUpdateRequest { Name = "Backend", IsActive = false };

            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(positionId)).ReturnsAsync(existingPosition);

            var relatedQuestion = new Question
            {
                Id = 10, IsActive = true,
                QuestionPositions = new List<QuestionPosition> { new QuestionPosition { PositionId = positionId } }
            };

            var questions = new List<Question> { relatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdatePositionAsync(positionId, request);

            relatedQuestion.IsActive.Should().BeFalse();
            relatedQuestion.UpdatedAt.Should().NotBeNull();
        }

        // Kiểm tra cascade reactivate: khi position bật lại → question có UpdatedAt != null được bật lại
        [Fact]
        public async Task UpdatePositionAsync_ShouldReactivateRelatedQuestions_WhenPositionIsActivated()
        {
            var positionId = 1;
            var existingPosition = new Position { Id = positionId, Name = "Backend", IsActive = false };
            var request = new PositionUpdateRequest { Name = "Backend", IsActive = true };

            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(positionId)).ReturnsAsync(existingPosition);

            var deactivatedQuestion = new Question
            {
                Id = 10, IsActive = false, UpdatedAt = DateTime.UtcNow.AddDays(-1),
                QuestionPositions = new List<QuestionPosition> { new QuestionPosition { PositionId = positionId } }
            };

            var questions = new List<Question> { deactivatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdatePositionAsync(positionId, request);

            deactivatedQuestion.IsActive.Should().BeTrue();
        }

        // Kiểm tra edge case: question có UpdatedAt = null (bị tắt thủ công) KHÔNG được bật lại khi cascade reactivate
        [Fact]
        public async Task UpdatePositionAsync_ShouldNotReactivateQuestions_WithNullUpdatedAt()
        {
            var positionId = 1;
            var existingPosition = new Position { Id = positionId, Name = "Backend", IsActive = false };
            var request = new PositionUpdateRequest { Name = "Backend", IsActive = true };

            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(positionId)).ReturnsAsync(existingPosition);

            var questionNullUpdated = new Question
            {
                Id = 10, IsActive = false, UpdatedAt = null,
                QuestionPositions = new List<QuestionPosition> { new QuestionPosition { PositionId = positionId } }
            };

            var questions = new List<Question> { questionNullUpdated }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.UpdatePositionAsync(positionId, request);

            questionNullUpdated.IsActive.Should().BeFalse();
        }

        #endregion

        #region SetPositionStatusAsync

        // Kiểm tra trả null khi position ID không tồn tại
        [Fact]
        public async Task SetPositionStatusAsync_ShouldReturnNull_WhenPositionNotFound()
        {
            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(It.IsAny<int>())).ReturnsAsync((Position?)null);

            var result = await _service.SetPositionStatusAsync(999, true);

            result.Should().BeNull();
        }

        // Kiểm tra bật position: IsActive = true, UpdatedAt được gán, SaveChanges gọi 1 lần
        [Fact]
        public async Task SetPositionStatusAsync_ShouldActivatePosition()
        {
            var position = new Position { Id = 1, Name = "Backend", IsActive = false };
            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(1)).ReturnsAsync(position);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(emptyQuestions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SetPositionStatusAsync(1, true);

            result!.IsActive.Should().BeTrue();
            result.UpdatedAt.Should().NotBeNull();
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        // Kiểm tra tắt position: IsActive = false
        [Fact]
        public async Task SetPositionStatusAsync_ShouldDeactivatePosition()
        {
            var position = new Position { Id = 1, Name = "Backend", IsActive = true };
            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(1)).ReturnsAsync(position);

            var emptyQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(emptyQuestions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.SetPositionStatusAsync(1, false);

            result!.IsActive.Should().BeFalse();
        }

        // Kiểm tra cascade deactivate question khi tắt position qua SetStatus
        [Fact]
        public async Task SetPositionStatusAsync_ShouldCascadeDeactivateQuestions()
        {
            var position = new Position { Id = 1, IsActive = true };
            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(1)).ReturnsAsync(position);

            var activeQuestion = new Question
            {
                Id = 10, IsActive = true,
                QuestionPositions = new List<QuestionPosition> { new QuestionPosition { PositionId = 1 } }
            };

            var questions = new List<Question> { activeQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.SetPositionStatusAsync(1, false);

            activeQuestion.IsActive.Should().BeFalse();
        }

        // Kiểm tra cascade reactivate question (có UpdatedAt != null) khi bật position qua SetStatus
        [Fact]
        public async Task SetPositionStatusAsync_ShouldCascadeReactivateQuestions()
        {
            var position = new Position { Id = 1, IsActive = false };
            _mockPositionRepo.Setup(r => r.GetPositionByIdAsync(1)).ReturnsAsync(position);

            var deactivatedQuestion = new Question
            {
                Id = 10, IsActive = false, UpdatedAt = DateTime.UtcNow.AddDays(-1),
                QuestionPositions = new List<QuestionPosition> { new QuestionPosition { PositionId = 1 } }
            };

            var questions = new List<Question> { deactivatedQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestionsTracking()).Returns(questions);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            await _service.SetPositionStatusAsync(1, true);

            deactivatedQuestion.IsActive.Should().BeTrue();
        }

        #endregion

        #region GetAffectedQuestionsAsync

        // Kiểm tra trả list rỗng khi bật position (không ảnh hưởng tiêu cực)
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldReturnEmptyList_WhenWillBeActiveIsTrue()
        {
            var result = await _service.GetAffectedQuestionsAsync(1, willBeActive: true);

            result.Should().BeEmpty();
        }

        // Kiểm tra trả danh sách question active sẽ bị ảnh hưởng khi tắt position
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldReturnAffectedQuestions_WhenDeactivating()
        {
            var question = new Question
            {
                Id = 10, Content = "Explain MVC", IsActive = true,
                QuestionPositions = new List<QuestionPosition> { new QuestionPosition { PositionId = 1 } }
            };

            var allQuestions = new List<Question> { question }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(allQuestions);

            var result = await _service.GetAffectedQuestionsAsync(1, willBeActive: false);

            result.Should().HaveCount(1);
            result[0].Content.Should().Be("Explain MVC");
        }

        // Kiểm tra question đã inactive không nằm trong danh sách bị ảnh hưởng
        [Fact]
        public async Task GetAffectedQuestionsAsync_ShouldExcludeInactiveQuestions()
        {
            var inactiveQuestion = new Question
            {
                Id = 10, IsActive = false,
                QuestionPositions = new List<QuestionPosition> { new QuestionPosition { PositionId = 1 } }
            };

            var allQuestions = new List<Question> { inactiveQuestion }.AsQueryable().BuildMock();
            _mockQuestionRepo.Setup(r => r.GetAllQuestions()).Returns(allQuestions);

            var result = await _service.GetAffectedQuestionsAsync(1, willBeActive: false);

            result.Should().BeEmpty();
        }

        #endregion

        #region GetNonExistingPositionIdsAsync

        // Kiểm tra trả về danh sách ID không tồn tại: input [1,2,99] → trả [99]
        [Fact]
        public async Task GetNonExistingPositionIdsAsync_ShouldReturnNonExistingIds()
        {
            var inputIds = new List<int> { 1, 2, 99 };
            _mockPositionRepo.Setup(r => r.GetNonExistingPositionIdsAsync(inputIds)).ReturnsAsync(new List<int> { 99 });

            var result = await _service.GetNonExistingPositionIdsAsync(inputIds);

            result.Should().HaveCount(1);
            result.Should().Contain(99);
        }

        // Kiểm tra trả list rỗng khi tất cả ID đều tồn tại
        [Fact]
        public async Task GetNonExistingPositionIdsAsync_ShouldReturnEmptyList_WhenAllExist()
        {
            var inputIds = new List<int> { 1, 2 };
            _mockPositionRepo.Setup(r => r.GetNonExistingPositionIdsAsync(inputIds)).ReturnsAsync(new List<int>());

            var result = await _service.GetNonExistingPositionIdsAsync(inputIds);

            result.Should().BeEmpty();
        }

        #endregion
    }
}
