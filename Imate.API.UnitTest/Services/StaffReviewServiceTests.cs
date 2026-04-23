using Moq;
using FluentAssertions;
using Imate.API.Business.Services.Staff;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Business.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Business.Exceptions;
using Imate.API.Presentation.ResponseModels.UserManagement;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces.Classification;
using Xunit;
using MediatR;

namespace Imate.API.UnitTest.Services
{
    public class StaffReviewServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IMediator> _mockMediator;
        private readonly StaffReviewService _service;
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IMentorRepository> _mockMentorRepo;
        private readonly Mock<IRecruiterRepository> _mockRecruiterRepo;
        private readonly Mock<ICompanyRepository> _mockCompanyRepo;

        public StaffReviewServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            
            _mockMediator = new Mock<IMediator>();

            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockMentorRepo = new Mock<IMentorRepository>();
            _mockRecruiterRepo = new Mock<IRecruiterRepository>();
            _mockCompanyRepo = new Mock<ICompanyRepository>();

            _mockUnitOfWork.Setup(u => u.Accounts).Returns(_mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Mentors).Returns(_mockMentorRepo.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters).Returns(_mockRecruiterRepo.Object);
            _mockUnitOfWork.Setup(u => u.Companies).Returns(_mockCompanyRepo.Object);

            _service = new StaffReviewService(_mockUnitOfWork.Object, _mockAuditLogService.Object, _mockMediator.Object);
        }

        #region Review Mentor Application
        [Fact]
        public async Task ReviewMentorApplicationAsync_ShouldApprove_WhenIsApprovedIsTrue()
        {
            // Arrange
            var accountId = 1;
            var staffId = 100;
            var account = new Account 
            { 
                Id = accountId, 
                Status = AccountStatus.PendingVerification,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } }
            };
            var mentor = new Mentor { AccountId = accountId, VerificationStatus = VerificationStatus.Pending };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockMentorRepo = new Mock<IMentorRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Mentors).Returns(mockMentorRepo.Object);
            
            mockAccountRepo.Setup(u => u.GetByIdForStatusUpdateAsync(accountId)).ReturnsAsync(account);
            mockMentorRepo.Setup(u => u.GetMentorByIdAsync(accountId)).ReturnsAsync(mentor);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.ReviewMentorApplicationAsync(accountId, true, "Approved", staffId);

            // Assert
            account.Status.Should().Be(AccountStatus.Active);
            mentor.VerificationStatus.Should().Be(VerificationStatus.Verified);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(staffId, AuditAction.Update, "Mentor", accountId, It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task ReviewMentorApplicationAsync_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdForStatusUpdateAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.ReviewMentorApplicationAsync(1, true, "Note", 100);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }
        #endregion

        #region Review Recruiter Application
        [Fact]
        public async Task ReviewRecruiterApplicationAsync_ShouldApproveAndCreateCompany_WhenApprovedAndCreateCompanyIsTrue()
        {
            // Arrange
            var accountId = 2;
            var staffId = 100;
            var account = new Account
            {
                Id = accountId,
                Status = AccountStatus.PendingVerification,
                Recruiter = new Recruiter { CompanyName = "Test Co", CompanyLogo = "logo.png", VerificationStatus = VerificationStatus.Pending }
            };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockCompanyRepo = new Mock<ICompanyRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Companies).Returns(mockCompanyRepo.Object);

            mockAccountRepo.Setup(u => u.GetByIdRecruiter(accountId)).ReturnsAsync(account);
            mockCompanyRepo.Setup(u => u.GetByNameAsync("Test Co")).ReturnsAsync((Company?)null);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.ReviewRecruiterApplicationAsync(accountId, true, "Approved", staffId, true);

            // Assert
            account.Status.Should().Be(AccountStatus.Active);
            account.Recruiter.VerificationStatus.Should().Be(VerificationStatus.Verified);
            _mockUnitOfWork.Verify(u => u.Companies.AddAsync(It.Is<Company>(c => c.Name == "Test Co")), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviewRecruiterApplicationAsync_ShouldReject_WhenIsApprovedIsFalse()
        {
            // Arrange
            var accountId = 2;
            var account = new Account
            {
                Id = accountId,
                Status = AccountStatus.PendingVerification,
                Recruiter = new Recruiter { VerificationStatus = VerificationStatus.Pending }
            };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(accountId)).ReturnsAsync(account);

            // Act
            await _service.ReviewRecruiterApplicationAsync(accountId, false, "Rejected", 100, false);

            // Assert
            account.Status.Should().Be(AccountStatus.PendingVerification); // Status doesn't change to Active on rejection
            account.Recruiter.VerificationStatus.Should().Be(VerificationStatus.Rejected);
        }
        #endregion

        #region Review Mentor Application (Additional)
        [Fact]
        public async Task ReviewMentorApplicationAsync_ShouldReject_WhenIsApprovedIsFalse()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId, Status = AccountStatus.PendingVerification, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } } };
            var mentor = new Mentor { AccountId = accountId, VerificationStatus = VerificationStatus.Pending };

            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdForStatusUpdateAsync(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(accountId)).ReturnsAsync(mentor);

            // Act
            await _service.ReviewMentorApplicationAsync(accountId, false, "Rejected", 100);

            // Assert
            account.Status.Should().Be(AccountStatus.PendingVerification);
            mentor.VerificationStatus.Should().Be(VerificationStatus.Rejected);
        }

        [Fact]
        public async Task ReviewMentorApplicationAsync_ShouldThrowBadRequest_WhenMissingMentorRole()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole>() };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdForStatusUpdateAsync(1)).ReturnsAsync(account);

            // Act
            var act = () => _service.ReviewMentorApplicationAsync(1, true, "Note", 100);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Tài khoản không có vai trò Mentor.");
        }

        [Fact]
        public async Task ReviewMentorApplicationAsync_ShouldThrowBadRequest_WhenStatusIsNotPending()
        {
            // Arrange
            var account = new Account 
            { 
                Id = 1, 
                Status = AccountStatus.Active, 
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } } 
            };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdForStatusUpdateAsync(1)).ReturnsAsync(account);

            // Act
            var act = () => _service.ReviewMentorApplicationAsync(1, true, "Note", 100);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Tài khoản không ở trạng thái chờ duyệt.");
        }

        [Fact]
        public async Task ReviewMentorApplicationAsync_ShouldSkipAuditLog_WhenStaffIdIsZero()
        {
            // Arrange
            var account = new Account { Id = 1, Status = AccountStatus.PendingVerification, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdForStatusUpdateAsync(1)).ReturnsAsync(account);

            // Act
            await _service.ReviewMentorApplicationAsync(1, true, "Note", 0);

            // Assert
            _mockAuditLogService.Verify(a => a.CreateAuditLogAsync(It.IsAny<int>(), It.IsAny<AuditAction>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>()), Times.Never);
        }

        [Fact]
        public async Task ReviewMentorApplicationAsync_ShouldSkipMentorUpdate_WhenMentorEntityNotFound()
        {
            // Arrange
            var account = new Account { Id = 1, Status = AccountStatus.PendingVerification, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdForStatusUpdateAsync(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(1)).ReturnsAsync((Mentor?)null);

            // Act
            await _service.ReviewMentorApplicationAsync(1, true, "Note", 100);

            // Assert (No exception, just skip update)
            account.Status.Should().Be(AccountStatus.Active);
        }
        #endregion

        #region Review Recruiter Application (Additional)
        [Fact]
        public async Task ReviewRecruiterApplicationAsync_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.ReviewRecruiterApplicationAsync(1, true, "Note", 100, false);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task ReviewRecruiterApplicationAsync_ShouldThrowBadRequest_WhenStatusIsNotPending()
        {
            // Arrange
            var account = new Account { Id = 1, Status = AccountStatus.Active };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);

            // Act
            var act = () => _service.ReviewRecruiterApplicationAsync(1, true, "Note", 100, false);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ReviewRecruiterApplicationAsync_ShouldSkipCompanyCreation_WhenExistingCompanyFound()
        {
            // Arrange
            var account = new Account
            {
                Id = 1,
                Status = AccountStatus.PendingVerification,
                Recruiter = new Recruiter { CompanyName = "Existing Co" }
            };
            var mockCompanyRepo = new Mock<ICompanyRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Companies).Returns(mockCompanyRepo.Object);
            mockCompanyRepo.Setup(u => u.GetByNameAsync("Existing Co")).ReturnsAsync(new Company());

            // Act
            await _service.ReviewRecruiterApplicationAsync(1, true, "Approved", 100, true);

            // Assert
            mockCompanyRepo.Verify(u => u.AddAsync(It.IsAny<Company>()), Times.Never);
        }

        [Fact]
        public async Task ReviewRecruiterApplicationAsync_ShouldSkipCompanyCreation_WhenCreateCompanyIsFalse()
        {
            // Arrange
            var account = new Account { Id = 1, Status = AccountStatus.PendingVerification, Recruiter = new Recruiter { CompanyName = "Test" } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);

            // Act
            await _service.ReviewRecruiterApplicationAsync(1, true, "Approved", 100, false);

            // Assert
            _mockUnitOfWork.Verify(u => u.Companies, Times.Never);
        }

        [Fact]
        public async Task ReviewRecruiterApplicationAsync_ShouldSkipRecruiterUpdate_WhenRecruiterEntityNotFound()
        {
            // Arrange
            var account = new Account { Id = 1, Status = AccountStatus.PendingVerification, Recruiter = null };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdRecruiter(1)).ReturnsAsync(account);

            // Act
            await _service.ReviewRecruiterApplicationAsync(1, true, "Approved", 100, false);

            // Assert
            account.Status.Should().Be(AccountStatus.Active);
        }
        #endregion
    }
}
