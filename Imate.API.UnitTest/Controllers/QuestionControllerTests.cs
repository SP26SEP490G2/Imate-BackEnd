using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers.QuestionBank;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;
using Imate.API.Presentation.ResponseModels.QuestionBank;
using Imate.API.Presentation.RequestModels.QuestionBank;
using Imate.API.Business.Helper;

namespace Imate.API.UnitTest.Controllers.QuestionBank
{
    public class QuestionControllerTests
    {
        private readonly Mock<IQuestionService> _mockQuestionService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly QuestionController _controller;

        public QuestionControllerTests()
        {
            _mockQuestionService = new Mock<IQuestionService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _controller = new QuestionController(_mockQuestionService.Object, _mockAuditLogService.Object);
        }

        private void SetupUser(int userId, string role = "Candidate")
        {
            var claims = new List<Claim> 
            { 
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("SubscriptionPackage", "Gói VIP")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task GetListHotQuestions_Success()
        {
            // Arrange
            var mockQuestions = new List<QuestionResponse.ListHotQuestion>
            {
                new QuestionResponse.ListHotQuestion { Id = 1, Content = "Hot Question 1" }
            };
            _mockQuestionService.Setup(s => s.GetListHotQuestionsAsync()).ReturnsAsync(mockQuestions);

            // Act
            var result = await _controller.GetListHotQuestions();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
            _mockQuestionService.Verify(s => s.GetListHotQuestionsAsync(), Times.Once);
        }

        [Fact]
        public async Task GetPublicSystemQuestionBanks_Success()
        {
            // Arrange
            var accountId = 1;
            SetupUser(accountId, "Candidate");
            var mockQuestions = new List<PublicSystemQuestionResponseModel>
            {
                new PublicSystemQuestionResponseModel { Id = 1, Content = "System Question 1" }
            };
            var request = new GetPublicSystemQuestionParams { PageNumber = 1, PageSize = 10 };
            
            var pagedResult = new PagedList<PublicSystemQuestionResponseModel>(mockQuestions, 1, 1, 10);
            _mockQuestionService.Setup(s => s.GetPublicSystemQuestionBanksWithPaginationAsync("Gói VIP", accountId, request))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetPublicSystemQuestionBanks(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _controller.Response.Headers.ContainsKey("X-Pagination").Should().BeTrue();
        }

        [Fact]
        public async Task GetAllSystemQuestionsForStaffAsync_Success()
        {
            // Arrange
            SetupUser(1, "Staff");
            var mockResults = new List<GetAllSystemQuestionsForStaffAsyncResponse>
            {
                new GetAllSystemQuestionsForStaffAsyncResponse { Id = 1, Content = "Staff Question" }
            };
            var pagedResult = new PagedList<GetAllSystemQuestionsForStaffAsyncResponse>(mockResults, 1, 1, 10);
            _mockQuestionService.Setup(s => s.GetAllSystemQuestionsForStaffAsync(It.IsAny<GetSystemQuestionParams>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAllSystemQuestionsForStaffAsync(new GetSystemQuestionParams());

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(pagedResult);
        }

        [Fact]
        public async Task CreateSystemQuestionForStaffAsync_Success()
        {
            // Arrange
            SetupUser(1, "Staff");
            var request = new CreateSystemQuestionForStaffRequest { Content = "New Question", Difficulty = DifficultyLevel.Medium };
            var createdQuestion = new Question { Id = 100, Content = "New Question", CreatorId = 1 };
            
            _mockQuestionService.Setup(s => s.CreateSystemQuestionForStaffAsync(request, 1))
                .ReturnsAsync(createdQuestion);

            // Act
            var result = await _controller.CreateSystemQuestionForStaffAsync(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _mockAuditLogService.Verify(s => s.CreateAuditLogAsync(1, AuditAction.Create, "Question", 100, It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task UpdateSystemQuestionForStaffAsync_Success()
        {
            // Arrange
            var questionId = 1;
            var request = new UpdateSystemQuestionForStaffRequest { Content = "Updated Content" };
            var updatedQuestion = new Question { Id = questionId, Content = "Updated Content" };

            _mockQuestionService.Setup(s => s.UpdateSystemQuestionForStaffAsync(questionId, request))
                .ReturnsAsync(updatedQuestion);

            // Act
            var result = await _controller.UpdateSystemQuestionForStaffAsync(questionId, request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _mockQuestionService.Verify(s => s.UpdateSystemQuestionForStaffAsync(questionId, request), Times.Once);
        }
        [Fact]
        public async Task GetPublicContributedQuestionBanks_Success()
        {
            // Arrange
            var accountId = 1;
            SetupUser(accountId, "Candidate");
            var mockResults = new List<PublicContributedQuestionResponseModel>
            {
                new PublicContributedQuestionResponseModel { Id = 1, Content = "Contributed Question" }
            };
            var request = new GetPublicContributedQuestionParams { PageNumber = 1, PageSize = 10 };
            var pagedResult = new PagedList<PublicContributedQuestionResponseModel>(mockResults, 1, 1, 10);
            
            _mockQuestionService.Setup(s => s.GetPublicContributedQuestionBanksWithPaginationAsync("Gói VIP", accountId, request))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetPublicContributedQuestionBanks(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _controller.Response.Headers.ContainsKey("X-Pagination").Should().BeTrue();
        }

        [Fact]
        public async Task GetSystemQuestionForStaffByIdAsync_Success()
        {
            // Arrange
            SetupUser(1);
            var questionId = 1;
            _mockQuestionService.Setup(s => s.GetSystemQuestionByIdAsync(questionId, 1))
                .ReturnsAsync(new GetAllSystemQuestionsForStaffAsyncResponse { Id = questionId });

            // Act
            var result = await _controller.GetSystemQuestionByIdAsync(questionId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task ContributeQuestion_Success()
        {
            // Arrange
            SetupUser(1, "Candidate");
            var request = new ContributeQuestionRequestModel { Content = "New Contribution" };

            // Act
            var result = await _controller.ContributeQuestion(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(201);
            _mockQuestionService.Verify(s => s.CreateContributedQuestionAsync(request, 1), Times.Once);
        }

        [Fact]
        public async Task GetMyContributedQuestionsAsync_Success()
        {
            // Arrange
            SetupUser(1, "Candidate");
            var mockResults = new List<GetMyContributedQuestionsResponse>
            {
                new GetMyContributedQuestionsResponse { Id = 1, Content = "My Question" }
            };
            var pagedResult = new PagedList<GetMyContributedQuestionsResponse>(mockResults, 1, 1, 10);
            _mockQuestionService.Setup(s => s.GetMyContributedQuestionsAsync(1, It.IsAny<GetMyContributedQuestionsParams>()))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetMyContributedQuestionsAsync(new GetMyContributedQuestionsParams());

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _controller.Response.Headers.ContainsKey("X-Pagination").Should().BeTrue();
        }

        [Fact]
        public async Task UpdateContributedQuestionStatusAsync_Success()
        {
            // Arrange
            SetupUser(1, "Staff");
            var questionId = 1;
            var status = true; // Approve
            _mockQuestionService.Setup(s => s.UpdateContributedQuestionStatusAsync(questionId, status, 1))
                .ReturnsAsync(new Question { Id = questionId });

            // Act
            var result = await _controller.UpdateContributedQuestionStatusAsync(questionId, status);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockQuestionService.Verify(s => s.UpdateContributedQuestionStatusAsync(questionId, status, 1), Times.Once);
        }

        [Fact]
        public async Task CreateSystemQuestionForStaffAsync_Unauthorized_WhenTokenInvalid()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new CreateSystemQuestionForStaffRequest { Content = "New Question" };

            // Act
            var result = await _controller.CreateSystemQuestionForStaffAsync(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task ContributeQuestion_Unauthorized_WhenTokenInvalid()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new ContributeQuestionRequestModel { Content = "New Contribution" };

            // Act
            var result = await _controller.ContributeQuestion(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetMyContributedQuestionsAsync_Unauthorized_WhenTokenInvalid()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.GetMyContributedQuestionsAsync(new GetMyContributedQuestionsParams());

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task UpdateContributedQuestionStatusAsync_Unauthorized_WhenTokenInvalid()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.UpdateContributedQuestionStatusAsync(1, true);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
