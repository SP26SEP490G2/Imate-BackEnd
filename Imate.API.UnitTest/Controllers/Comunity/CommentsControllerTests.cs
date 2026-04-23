using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers.Comunity;
using Imate.API.Business.Interfaces.Comunity;
using Imate.API.Presentation.RequestModels.Comunity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Imate.API.UnitTest.Controllers.Comunity
{
    public class CommentsControllerTests
    {
        private readonly Mock<ICommentService> _mockCommentService;
        private readonly CommentsController _controller;

        public CommentsControllerTests()
        {
            _mockCommentService = new Mock<ICommentService>();
            _controller = new CommentsController(_mockCommentService.Object);
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
        public async Task CreateComment_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var request = new CreateCommentRequestModel { Content = "New comment", QuestionId = 101 };
            _mockCommentService.Setup(s => s.CreateCommentAsync(userId, request)).ReturnsAsync(1);

            // Act
            var result = await _controller.CreateComment(request);

            // Assert
            var statusCodeResult = result.Should().BeOfType<StatusCodeResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task UpdateComment_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var commentId = 10;
            var request = new UpdateCommentRequestModel { Content = "Updated content" };

            // Act
            var result = await _controller.UpdateComment(commentId, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mockCommentService.Verify(s => s.UpdateCommentAsync(commentId, userId, request), Times.Once);
        }

        [Fact]
        public async Task VoteComment_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var commentId = 10;
            var request = new VoteCommentRequestModel { IsUpvote = true }; 

            // Act
            var result = await _controller.VoteComment(commentId, request);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mockCommentService.Verify(s => s.ToggleVoteAsync(commentId, userId, request), Times.Once);
        }

        [Fact]
        public async Task DeleteComment_Success()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var commentId = 10;

            // Act
            var result = await _controller.DeleteComment(commentId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _mockCommentService.Verify(s => s.DeleteCommentAsync(commentId, userId), Times.Once);
        }
        [Fact]
        public async Task CreateComment_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new CreateCommentRequestModel { Content = "New comment", QuestionId = 101 };

            // Act
            var result = await _controller.CreateComment(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task UpdateComment_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new UpdateCommentRequestModel { Content = "Updated content" };

            // Act
            var result = await _controller.UpdateComment(10, request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task VoteComment_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var request = new VoteCommentRequestModel { IsUpvote = true };

            // Act
            var result = await _controller.VoteComment(10, request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task DeleteComment_Unauthorized_WhenNoUserId()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };

            // Act
            var result = await _controller.DeleteComment(10);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
