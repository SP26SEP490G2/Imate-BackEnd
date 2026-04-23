using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers;
using Imate.API.Business.Interfaces;
using Imate.API.Presentation.RequestModels.PracticeTest;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

using Imate.API.Presentation.ResponseModels.PracticeTest;

namespace Imate.API.UnitTest.Controllers
{
    public class PracticeTestHistoryControllerTests
    {
        private readonly Mock<IPracticeTestHistoryService> _mockHistoryService;
        private readonly PracticeTestHistoryController _controller;

        public PracticeTestHistoryControllerTests()
        {
            _mockHistoryService = new Mock<IPracticeTestHistoryService>();
            _controller = new PracticeTestHistoryController(_mockHistoryService.Object);
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
        public async Task SubmitTest_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var request = new SubmitPracticeTestRequest(); // Assuming parameters are empty for basic success test
            _mockHistoryService.Setup(s => s.SubmitTestAsync(userId, request)).ReturnsAsync(100);

            // Act
            var result = await _controller.SubmitTest(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _mockHistoryService.Verify(s => s.SubmitTestAsync(userId, request), Times.Once);
        }

        [Fact]
        public async Task GetHistory_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            _mockHistoryService.Setup(s => s.GetHistoryAsync(userId)).ReturnsAsync(new List<TestHistoryItemResponse>());

            // Act
            var result = await _controller.GetHistory();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        }
        [Fact]
        public async Task SubmitTest_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new SubmitPracticeTestRequest();

            // Act
            var result = await _controller.SubmitTest(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task GetHistory_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.GetHistory();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
