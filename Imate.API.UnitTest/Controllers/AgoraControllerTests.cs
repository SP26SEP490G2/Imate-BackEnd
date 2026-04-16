using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Business.Interfaces;
using Imate.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.Business.Services;

namespace Imate.API.UnitTest.Controllers
{
    public class AgoraControllerTests
    {
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly Mock<ISystemConfigService> _mockSysConfig;
        private readonly AgoraController _controller;

        public AgoraControllerTests()
        {
            _mockConfig = new Mock<IConfiguration>();
            _mockUow = new Mock<IUnitOfWork>();
            _mockSysConfig = new Mock<ISystemConfigService>();

            // Setup default config (must be 32 hex chars for Agora SDK)
            _mockConfig.Setup(c => c["Agora:AppId"]).Returns("0123456789abcdef0123456789abcdef"); 
            _mockConfig.Setup(c => c["Agora:AppCertificate"]).Returns("abcdef0123456789abcdef0123456789");
            _mockSysConfig.Setup(s => s.GetAgoraTokenExpirationHoursAsync()).ReturnsAsync(1);

            _controller = new AgoraController(_mockConfig.Object, _mockUow.Object, _mockSysConfig.Object);
        }

        private void SetupUser(int userId)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = principal };
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task GetTokenForBooking_CandidateSuccess()
        {
            // Arrange
            var bookingId = 1;
            var userId = 10;
            SetupUser(userId);

            var booking = new Booking
            {
                Id = bookingId,
                CandidateId = userId,
                Mentor = new Mentor { AccountId = 20 },
                StartTime = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            _mockUow.Setup(u => u.Bookings.GetBookingByIdAsync(bookingId)).ReturnsAsync(booking);

            // Act
            var result = await _controller.GetTokenForBooking(bookingId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AgoraTokenResponse>().Subject;
            response.ChannelName.Should().Be(bookingId.ToString());
            response.Uid.Should().Be((uint)userId);
            response.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetTokenForBooking_MentorSuccess()
        {
            // Arrange
            var bookingId = 1;
            var userId = 20;
            SetupUser(userId);

            var booking = new Booking
            {
                Id = bookingId,
                CandidateId = 10,
                Mentor = new Mentor { AccountId = userId },
                StartTime = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            _mockUow.Setup(u => u.Bookings.GetBookingByIdAsync(bookingId)).ReturnsAsync(booking);

            // Act
            var result = await _controller.GetTokenForBooking(bookingId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<AgoraTokenResponse>().Subject;
            response.Uid.Should().Be((uint)userId);
            response.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task GetTokenForBooking_Forbidden()
        {
            // Arrange
            var bookingId = 1;
            var userId = 99; // Not candidate, not mentor
            SetupUser(userId);

            var booking = new Booking
            {
                Id = bookingId,
                CandidateId = 10,
                Mentor = new Mentor { AccountId = 20 },
                StartTime = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            _mockUow.Setup(u => u.Bookings.GetBookingByIdAsync(bookingId)).ReturnsAsync(booking);

            // Act
            var result = await _controller.GetTokenForBooking(bookingId);

            // Assert
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task GetTokenForBooking_BookingNotFound()
        {
            // Arrange
            var bookingId = 1;
            SetupUser(10);
            _mockUow.Setup(u => u.Bookings.GetBookingByIdAsync(bookingId)).ReturnsAsync((Booking?)null);

            // Act
            var result = await _controller.GetTokenForBooking(bookingId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetTokenForBooking_ConfigMissing()
        {
            // Arrange
            var bookingId = 1;
            var userId = 10;
            SetupUser(userId);

            var booking = new Booking
            {
                Id = bookingId,
                CandidateId = userId,
                Mentor = new Mentor { AccountId = 20 },
                StartTime = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            _mockUow.Setup(u => u.Bookings.GetBookingByIdAsync(bookingId)).ReturnsAsync(booking);
            _mockConfig.Setup(c => c["Agora:AppId"]).Returns(""); // Missing AppId

            // Act
            var result = await _controller.GetTokenForBooking(bookingId);

            // Assert
            result.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetTokenForBooking_Unauthorized_NoClaims()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext 
            { 
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) } 
            };

            // Act
            var result = await _controller.GetTokenForBooking(1);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}
