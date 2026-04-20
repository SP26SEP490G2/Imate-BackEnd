using Moq;
using FluentAssertions;
using Imate.API.Business.Services;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Business.Exceptions;
using Imate.API.Presentation.RequestModels.AuditLog;
using MockQueryable.Moq;
using System.Text.Json;
using Xunit;
using MockQueryable;

namespace Imate.API.UnitTest.Services
{
    public class AuditLogServiceTests
    {
        private readonly Mock<IAuditLogRepository> _mockAuditLogRepo;
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly AuditLogService _service;

        public AuditLogServiceTests()
        {
            _mockAuditLogRepo = new Mock<IAuditLogRepository>();
            _mockAccountRepo = new Mock<IAccountRepository>();
            _service = new AuditLogService(_mockAuditLogRepo.Object, _mockAccountRepo.Object);
        }

        #region Monitor Audit Logs
        [Fact]
        public async Task GetFilterOptionsAsync_ShouldReturnOptions_Successfully()
        {
            // Arrange
            var auditLogs = new List<AuditLog>
            {
                new AuditLog { User = new Account { FullName = "Alice" }, Action = AuditAction.Create, EntityType = "User" },
                new AuditLog { User = new Account { FullName = "Bob" }, Action = AuditAction.Update, EntityType = "Job" },
                new AuditLog { User = null, Action = AuditAction.Delete, EntityType = "Job" } // Null user
            }.AsQueryable().BuildMock();

            _mockAuditLogRepo.Setup(r => r.GetAllAuditLogs()).Returns(auditLogs);

            // Act
            var result = await _service.GetFilterOptionsAsync();

            // Assert
            result.Should().NotBeNull();
            result.StaffNames.Should().HaveCount(2).And.ContainInOrder("Alice", "Bob");
            result.Actions.Should().HaveCount(3).And.Contain(new[] { "Create", "Update", "Delete" });
            result.EntityTypes.Should().HaveCount(2).And.ContainInOrder("Job", "User");
        }

        [Fact]
        public async Task GetAuditLogsAsync_ShouldReturnLogs_WithPagination_WhenNoFiltersApplied()
        {
            // Arrange
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = 1, ActionTime = DateTimeOffset.UtcNow },
                new AuditLog { Id = 2, ActionTime = DateTimeOffset.UtcNow.AddMinutes(-5) }
            }.AsQueryable().BuildMock();

            _mockAuditLogRepo.Setup(r => r.GetAllAuditLogs()).Returns(logs);
            var query = new AuditLogParams { PageNumber = 1, PageSize = 10 };

            // Act
            var result = await _service.GetAuditLogsAsync(query);

            // Assert
            result.TotalCount.Should().Be(2);
            result.Items.Should().HaveCount(2);
            result.Items.First().Id.Should().Be(1);
        }

        [Fact]
        public async Task GetAuditLogsAsync_ShouldFilterByStaffNameAndEntityTypeAndAction()
        {
            // Arrange
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = 1, User = new Account { FullName = "John" }, EntityType = "Task", Action = AuditAction.Create },
                new AuditLog { Id = 2, User = new Account { FullName = "Jane" }, EntityType = "Task", Action = AuditAction.Update },
                new AuditLog { Id = 3, User = new Account { FullName = "John" }, EntityType = "Project", Action = AuditAction.Create }
            }.AsQueryable().BuildMock();

            _mockAuditLogRepo.Setup(r => r.GetAllAuditLogs()).Returns(logs);
            var query = new AuditLogParams
            {
                StaffName = "John",
                EntityType = "Task",
                Action = AuditAction.Create,
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _service.GetAuditLogsAsync(query);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Single().Id.Should().Be(1);
        }

        [Fact]
        public async Task GetAuditLogsAsync_ShouldFilterBySearchTerm_InVariousFields()
        {
            // Arrange
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = 1, User = new Account { FullName = "KeywordUser" }, EntityType = "Task", Action = AuditAction.Create },
                new AuditLog { Id = 2, User = new Account { Email = "keyword@test.com" }, EntityType = "Task", Action = AuditAction.Update },
                new AuditLog { Id = 3, User = null, EntityType = "KeywordEntity", Action = AuditAction.Delete },
                new AuditLog { Id = 4, User = null, EntityType = "Task", Action = AuditAction.RejectQuestion, EntityId = 123 }, 
                new AuditLog { Id = 5, User = null, EntityType = "Task", Action = AuditAction.Update, EntityId = 999 } 
            }.AsQueryable().BuildMock();

            _mockAuditLogRepo.Setup(r => r.GetAllAuditLogs()).Returns(logs);

            var r1 = await _service.GetAuditLogsAsync(new AuditLogParams { SearchTerm = "keyworduser" });
            r1.TotalCount.Should().Be(1);
            r1.Items.First().Id.Should().Be(1);

            var r2 = await _service.GetAuditLogsAsync(new AuditLogParams { SearchTerm = "keyword@" });
            r2.TotalCount.Should().Be(1);
            r2.Items.First().Id.Should().Be(2);

            var r3 = await _service.GetAuditLogsAsync(new AuditLogParams { SearchTerm = "keywordentity" });
            r3.TotalCount.Should().Be(1);
            r3.Items.First().Id.Should().Be(3);

            var r4 = await _service.GetAuditLogsAsync(new AuditLogParams { SearchTerm = "RejectQuestion" });
            r4.TotalCount.Should().Be(1);
            r4.Items.First().Id.Should().Be(4);

            var r5 = await _service.GetAuditLogsAsync(new AuditLogParams { SearchTerm = "999" });
            r5.TotalCount.Should().Be(1);
            r5.Items.First().Id.Should().Be(5);
        }

        [Fact]
        public async Task GetAuditLogsAsync_ShouldFilterByDateRange()
        {
            // Arrange
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = 1, ActionTime = new DateTimeOffset(2023, 1, 15, 0, 0, 0, TimeSpan.Zero) },
                new AuditLog { Id = 2, ActionTime = new DateTimeOffset(2023, 2, 15, 0, 0, 0, TimeSpan.Zero) },
                new AuditLog { Id = 3, ActionTime = new DateTimeOffset(2023, 3, 15, 0, 0, 0, TimeSpan.Zero) }
            }.AsQueryable().BuildMock();

            _mockAuditLogRepo.Setup(r => r.GetAllAuditLogs()).Returns(logs);
            var query = new AuditLogParams
            {
                FromDate = new DateTime(2023, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                ToDate = new DateTime(2023, 2, 28, 0, 0, 0, DateTimeKind.Utc)
            };

            // Act
            var result = await _service.GetAuditLogsAsync(query);

            // Assert
            result.TotalCount.Should().Be(1);
            result.Items.Single().Id.Should().Be(2);
        }

        [Theory]
        [InlineData("actiontime", "asc", 1, 2, 3)]
        [InlineData("actiontime", "desc", 3, 2, 1)]
        [InlineData("staffname", "asc", 2, 1, 3)]
        [InlineData("staffname", "desc", 3, 1, 2)]
        [InlineData("entitytype", "asc", 1, 3, 2)]
        [InlineData("entitytype", "desc", 2, 3, 1)]
        [InlineData("unknown", "asc", 3, 2, 1)]     // Default to actiontime desc
        public async Task GetAuditLogsAsync_ShouldSortCorrectly(string sortBy, string sortOrder, int expectedFirst, int expectedSecond, int expectedThird)
        {
            // Arrange
            var logs = new List<AuditLog>
            {
                new AuditLog { Id = 1, ActionTime = new DateTimeOffset(2023, 1, 1, 0,0,0,TimeSpan.Zero), User = new Account{ FullName = "B_User" }, EntityType = "A_Type" },
                new AuditLog { Id = 2, ActionTime = new DateTimeOffset(2023, 2, 1, 0,0,0,TimeSpan.Zero), User = new Account{ FullName = "A_User" }, EntityType = "C_Type" },
                new AuditLog { Id = 3, ActionTime = new DateTimeOffset(2023, 3, 1, 0,0,0,TimeSpan.Zero), User = new Account{ FullName = "C_User" }, EntityType = "B_Type" }
            }.AsQueryable().BuildMock();

            _mockAuditLogRepo.Setup(r => r.GetAllAuditLogs()).Returns(logs);
            var query = new AuditLogParams { SortBy = sortBy, SortOrder = sortOrder };

            // Act
            var result = await _service.GetAuditLogsAsync(query);

            // Assert
            var resultIds = result.Items.Select(i => i.Id).ToList();
            resultIds[0].Should().Be(expectedFirst);
            resultIds[1].Should().Be(expectedSecond);
            resultIds[2].Should().Be(expectedThird);
        }

        #endregion

    }
}