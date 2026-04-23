using Moq;
using FluentAssertions;
using Imate.API.Presentation.Controllers;
using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Imate.API.UnitTest.Controllers
{
    public class SystemConfigControllerTests
    {
        private readonly Mock<ISystemConfigService> _mockService;
        private readonly Mock<IAuditLogService> _mockAuditService;
        private readonly Mock<IUnitOfWork> _mockUow;
        private readonly SystemConfigController _controller;

        public SystemConfigControllerTests()
        {
            _mockService = new Mock<ISystemConfigService>();
            _mockAuditService = new Mock<IAuditLogService>();
            _mockUow = new Mock<IUnitOfWork>();
            _controller = new SystemConfigController(_mockService.Object, _mockAuditService.Object, _mockUow.Object);
        }

        private void SetupUser(int userId, string role = "Admin")
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
        public async Task GetAllConfigs_Success()
        {
            // Arrange
            var mockConfigs = new List<SystemConfigResponse> { new SystemConfigResponse { Key = "TestKey", Value = "TestValue" } };
            _mockService.Setup(s => s.GetAllConfigsAsync()).ReturnsAsync(mockConfigs);

            // Act
            var result = await _controller.GetAllConfigs();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task GetConfigByKey_Success()
        {
            // Arrange
            var key = "TestKey";
            var mockConfig = new SystemConfigResponse { Key = key, Value = "TestValue" };
            _mockService.Setup(s => s.GetConfigByKeyAsync(key)).ReturnsAsync(mockConfig);

            // Act
            var result = await _controller.GetConfigByKey(key);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task UpdateConfig_Success()
        {
            // Arrange
            SetupUser(1);
            var key = "TestKey";
            var newValue = "NewValue";
            var request = new UpdateSystemConfigRequest { Value = newValue };
            
            var existingConfig = new SystemConfig { Id = 1, Key = key, Value = "OldValue", Description = "Desc" };
            var updatedConfigResponse = new SystemConfigResponse { Key = key, Value = newValue, Description = "Desc" };

            _mockUow.Setup(u => u.SystemConfigs.GetByKeyAsync(key)).ReturnsAsync(existingConfig);
            _mockService.Setup(s => s.UpdateConfigAsync(key, newValue)).ReturnsAsync(updatedConfigResponse);

            // Act
            var result = await _controller.UpdateConfig(key, request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            _mockAuditService.Verify(a => a.CreateAuditLogAsync(
                1, 
                AuditAction.Update, 
                "SystemConfig", 
                1, 
                It.IsAny<object>(), 
                It.IsAny<object>()), 
                Times.Once);
        }

        [Fact]
        public async Task GetConfigByKey_NotFound()
        {
            // Arrange
            var key = "NonExistent";
            _mockService.Setup(s => s.GetConfigByKeyAsync(key)).ReturnsAsync((SystemConfigResponse)null);

            // Act
            var result = await _controller.GetConfigByKey(key);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateConfig_NoAuditLog_WhenUserNotAuthenticated()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var key = "TestKey";
            var request = new UpdateSystemConfigRequest { Value = "NewValue" };
            
            var existingConfig = new SystemConfig { Id = 1, Key = key, Value = "OldValue" };
            var updatedConfigResponse = new SystemConfigResponse { Key = key, Value = "NewValue" };

            _mockUow.Setup(u => u.SystemConfigs.GetByKeyAsync(key)).ReturnsAsync(existingConfig);
            _mockService.Setup(s => s.UpdateConfigAsync(key, "NewValue")).ReturnsAsync(updatedConfigResponse);

            // Act
            await _controller.UpdateConfig(key, request);

            // Assert
            _mockAuditService.Verify(a => a.CreateAuditLogAsync(
                It.IsAny<int>(), 
                It.IsAny<AuditAction>(), 
                It.IsAny<string>(), 
                It.IsAny<int>(), 
                It.IsAny<object>(), 
                It.IsAny<object>()), 
                Times.Never);
        }
    }
}
