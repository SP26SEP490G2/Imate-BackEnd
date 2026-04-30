using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers.Comunity;
using Imate.API.Business.Interfaces.Comunity;
using Imate.API.Presentation.RequestModels.Comunity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Imate.API.Business.Exceptions;
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

        [Fact]
        public async Task CreateComment_BadRequest_WhenContentIsEmpty()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var request = new CreateCommentRequestModel { Content = "", QuestionId = 101 };
            
            _mockCommentService.Setup(s => s.CreateCommentAsync(userId, request))
                .ThrowsAsync(new BadRequestException("Content empty"));

            // Act
            var result = await _controller.CreateComment(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateComment_NotFound_WhenIdInvalid()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var commentId = 999;
            var request = new UpdateCommentRequestModel { Content = "Updated" };
            
            _mockCommentService.Setup(s => s.UpdateCommentAsync(commentId, userId, request))
                .ThrowsAsync(new KeyNotFoundException("Comment not found"));

            // Act
            var result = await _controller.UpdateComment(commentId, request);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteComment_Forbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var commentId = 10;
            
            _mockCommentService.Setup(s => s.DeleteCommentAsync(commentId, userId))
                .ThrowsAsync(new UnauthorizedAccessException("Not owner"));

            // Act
            var result = await _controller.DeleteComment(commentId);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateComment_BadRequest_WhenContentIsWhitespaceOnly()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var request = new CreateCommentRequestModel { Content = "   ", QuestionId = 101 };
            
            _mockCommentService.Setup(s => s.CreateCommentAsync(userId, request))
                .ThrowsAsync(new BadRequestException("Content cannot be whitespace only"));

            // Act
            var result = await _controller.CreateComment(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateComment_Forbidden_WhenUserIsNotOwner()
        {
            // Arrange
            var userId = 2; // User B
            SetupUser(userId);
            var commentId = 10; // Comment owned by User A
            var request = new UpdateCommentRequestModel { Content = "Hacked content" };

            _mockCommentService.Setup(s => s.UpdateCommentAsync(commentId, userId, request))
                .ThrowsAsync(new UnauthorizedAccessException("Not the comment owner"));

            // Act
            var result = await _controller.UpdateComment(commentId, request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task CreateComment_BadRequest_WhenQuestionIdNotExists()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var request = new CreateCommentRequestModel { Content = "Valid content", QuestionId = 99999 };

            _mockCommentService.Setup(s => s.CreateCommentAsync(userId, request))
                .ThrowsAsync(new BadRequestException("Question not found"));

            // Act
            var result = await _controller.CreateComment(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task VoteComment_Forbidden_WhenSelfVoting()
        {
            // Arrange
            var userId = 1;
            SetupUser(userId);
            var commentId = 10;
            var request = new VoteCommentRequestModel { IsUpvote = true };

            _mockCommentService.Setup(s => s.ToggleVoteAsync(commentId, userId, request))
                .ThrowsAsync(new UnauthorizedAccessException("Cannot vote on your own comment"));

            // Act
            var result = await _controller.VoteComment(commentId, request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(403);
        }
    }
}
