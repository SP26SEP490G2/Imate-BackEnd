using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers.QuestionBank;
using Imate.API.Business.Interfaces.QuestionBank;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;
using Imate.API.Presentation.RequestModels.QuestionBank;
using Imate.API.Presentation.ResponseModels.QuestionBank;

namespace Imate.API.UnitTest.Controllers.QuestionBank
{
    public class SavedQuestionsControllerTests
    {
        private readonly Mock<ISavedQuestionService> _mockSavedQuestionService;
        private readonly SavedQuestionsController _controller;

        public SavedQuestionsControllerTests()
        {
            _mockSavedQuestionService = new Mock<ISavedQuestionService>();
            _controller = new SavedQuestionsController(_mockSavedQuestionService.Object);
        }

        private void SetupUser(int userId, string role = "Candidate")
        {
            var claims = new List<Claim> 
            { 
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task ToggleSaveQuestion_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var request = new SaveQuestionRequestModel { QuestionId = 101 };
            _mockSavedQuestionService.Setup(s => s.ToggleSaveQuestionAsync(userId, 101))
                .ReturnsAsync(new SaveQuestionResponseModel { IsSaved = true, Message = "Question saved successfully." });

            // Act
            var result = await _controller.ToggleSaveQuestion(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _mockSavedQuestionService.Verify(s => s.ToggleSaveQuestionAsync(userId, 101), Times.Once);
        }

        [Fact]
        public async Task GetSavedSystemQuestions_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var mockQuestions = new List<PublicSystemQuestionResponseModel>
            {
                new PublicSystemQuestionResponseModel { Id = 101, Content = "Saved System Question" }
            };
            _mockSavedQuestionService.Setup(s => s.GetSavedSystemQuestionsAsync(userId))
                .ReturnsAsync(mockQuestions);

            // Act
            var result = await _controller.GetSavedSystemQuestions();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(mockQuestions);
        }

        [Fact]
        public async Task GetSavedContributedQuestions_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var mockQuestions = new List<PublicContributedQuestionResponseModel>
            {
                new PublicContributedQuestionResponseModel { Id = 201, Content = "Saved Contributed Question" }
            };
            _mockSavedQuestionService.Setup(s => s.GetSavedContributedQuestionsAsync(userId))
                .ReturnsAsync(mockQuestions);

            // Act
            var result = await _controller.GetSavedContributedQuestions();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(mockQuestions);
        }

        [Fact]
        public async Task GetSavedSystemQuestions_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.GetSavedSystemQuestions();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetSavedContributedQuestions_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.GetSavedContributedQuestions();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task ToggleSaveQuestion_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new SaveQuestionRequestModel { QuestionId = 101 };

            // Act
            var result = await _controller.ToggleSaveQuestion(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
