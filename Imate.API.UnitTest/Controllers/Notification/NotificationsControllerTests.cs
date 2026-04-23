using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers.Notification;
using Imate.API.Business.Interfaces.Notification;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Imate.API.UnitTest.Controllers.Notification
{
    public class NotificationsControllerTests
    {
        private readonly Mock<ISystemNotificationService> _mockNotificationService;
        private readonly NotificationsController _controller;

        public NotificationsControllerTests()
        {
            _mockNotificationService = new Mock<ISystemNotificationService>();
            _controller = new NotificationsController(_mockNotificationService.Object);
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
        public async Task GetMyNotifications_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var mockNotifications = new List<object> { new { Id = 1, Content = "Test Notification" } };
            _mockNotificationService.Setup(s => s.GetNotificationsForUserAsync(userId))
                .ReturnsAsync(mockNotifications);

            // Act
            var result = await _controller.GetMyNotifications();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(mockNotifications);
        }

        [Fact]
        public async Task GetMyNotifications_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.GetMyNotifications();

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
