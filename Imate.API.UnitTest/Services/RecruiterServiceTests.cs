using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Recruiters;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Business.Exceptions;
using Imate.API.Presentation.RequestModels.Recruiters;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class RecruiterServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IAwsS3StorageService> _mockS3Service;
        private readonly RecruiterService _service;

        public RecruiterServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockS3Service = new Mock<IAwsS3StorageService>();
            _service = new RecruiterService(_mockUnitOfWork.Object, _mockAuditLogService.Object, _mockEmailService.Object, _mockS3Service.Object);
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldCreateNewRecruiter_WhenRecruiterDoesNotExist()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } }
            };
            var request = new SubmitRecruiterProfileRequest
            {
                CompanyName = "Imate Co",
                Phone = "123456789",
                Industry = "Tech"
            };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockRecruiterRepo = new Mock<IRecruiterRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters).Returns(mockRecruiterRepo.Object);

            mockAccountRepo.Setup(u => u.GetByIdRecruiter(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.SubmitRecruiterProfileAsync(accountId, request);

            // Assert
            mockRecruiterRepo.Verify(u => u.Create(It.Is<Recruiter>(r => r.CompanyName == "Imate Co" && r.AccountId == accountId)), Times.Once);
            account.Status.Should().Be(AccountStatus.PendingVerification);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenCompanyNameIsMissing()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } }
            };
            var request = new SubmitRecruiterProfileRequest { CompanyName = "", Phone = "123" };

            var mockAccountRepo = new Mock<IAccountRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            mockAccountRepo.Setup(u => u.GetByIdRecruiter(accountId)).ReturnsAsync(account);

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Tên công ty không được để trống.");
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldUpdate_WhenRecruiterExists()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } },
                Recruiter = new Recruiter { CompanyName = "Old Co", Phone = "000" }
            };
            var request = new SubmitRecruiterProfileRequest
            {
                CompanyName = "Updated Co",
                Phone = "111"
            };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockRecruiterRepo = new Mock<IRecruiterRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters).Returns(mockRecruiterRepo.Object);
            mockAccountRepo.Setup(u => u.GetByIdRecruiter(accountId)).ReturnsAsync(account);

            // Act
            await _service.SubmitRecruiterProfileAsync(accountId, request);

            // Assert
            account.Recruiter.CompanyName.Should().Be("Updated Co");
            account.Recruiter.Phone.Should().Be("111");
            mockRecruiterRepo.Verify(u => u.Update(account.Recruiter), Times.Once);
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenRequestIsNull()
        {
            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, null!);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Dữ liệu hồ sơ Recruiter không hợp lệ.");
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, new SubmitRecruiterProfileRequest());

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenNotARecruiter()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, new SubmitRecruiterProfileRequest());

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Chỉ tài khoản Recruiter mới có thể nộp hồ sơ Recruiter.");
        }

        [Fact]
        public async Task SubmitRecruiterProfileAsync_ShouldThrowBadRequest_WhenPhoneIsMissing()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            var request = new SubmitRecruiterProfileRequest { CompanyName = "Co", Phone = "" };

            // Act
            var act = () => _service.SubmitRecruiterProfileAsync(1, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Số điện thoại không được để trống.");
        }
    }
}
