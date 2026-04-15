using Moq;
using FluentAssertions;
using Imate.API.Business.Services.UserManagement;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Business.Exceptions;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class AccountServiceTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IAwsS3StorageService> _mockS3Service;
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly AccountService _service;

        public AccountServiceTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockS3Service = new Mock<IAwsS3StorageService>();
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockRoleService = new Mock<IRoleService>();
            _service = new AccountService(_mockAccountRepo.Object, _mockUnitOfWork.Object, _mockRoleService.Object, _mockS3Service.Object);
        }

        #region Submit Mentor Application
        [Fact]
        public async Task SubmitMentorProfileAsync_ShouldCreateNewMentor_WhenMentorDoesNotExist()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } }
            };
            var request = new UpdateMentorProfileRequest
            {
                Bio = "New Bio",
                Phone = "123456789",
                PricePerSession = 50000
            };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockMentorRepo = new Mock<IMentorRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Mentors).Returns(mockMentorRepo.Object);

            mockAccountRepo.Setup(u => u.GetByIdMentor(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.SaveChangesAsync()).Returns(Task.CompletedTask);

            // Act
            await _service.SubmitMentorProfileAsync(accountId, request);

            // Assert
            mockMentorRepo.Verify(u => u.Create(It.Is<Mentor>(m => m.Bio == "New Bio" && m.AccountId == accountId)), Times.Once);
            account.Status.Should().Be(AccountStatus.PendingVerification);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task SubmitMentorProfileAsync_ShouldThrowBadRequest_WhenUserIsNotMentor()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } }
            };
            var request = new UpdateMentorProfileRequest();

            var mockAccountRepo = new Mock<IAccountRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            mockAccountRepo.Setup(u => u.GetByIdMentor(accountId)).ReturnsAsync(account);

            // Act
            var act = () => _service.SubmitMentorProfileAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Chỉ tài khoản Mentor mới có thể nộp hồ sơ Mentor.");
        }
        #endregion

        #region View Mentor Details
        [Fact]
        public async Task GetAccountDetailMentor_ShouldReturnResponse_WhenAccountIsMentor()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                FullName = "Mentor Name",
                Email = "mentor@test.com",
                Status = AccountStatus.Active,
                Mentor = new Mentor { Phone = "123", Bio = "Bio", PricePerSession = 100, AvgRatings = 5 },
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } }
            };

            var mockBookingRepo = new Mock<IBookingRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Bookings).Returns(mockBookingRepo.Object);
            
            mockBookingRepo.Setup(r => r.GetMappedReviewsByMentorIdAsync(accountId))
                .ReturnsAsync(new List<ReviewResponseModel>());
            
            mockBookingRepo.Setup(r => r.CountCompletedBookingsByMentorIdAsync(accountId))
                .ReturnsAsync(10);

            // Act
            var result = await _service.GetAccountDetailMentor(accountId);

            // Assert
            result.Should().NotBeNull();
            result.FullName.Should().Be("Mentor Name");
            result.TotalCompletedSessions.Should().Be(10);
        }
        #endregion

        [Fact]
        public async Task SubmitMentorProfileAsync_ShouldUpdate_WhenMentorExists()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } },
                Mentor = new Mentor { Bio = "Old Bio", MentorPositions = new List<MentorPosition>() }
            };
            var request = new UpdateMentorProfileRequest
            {
                Bio = "Updated Bio",
                PositionIds = new List<int> { 1, 2 }
            };

            var mockAccountRepo = new Mock<IAccountRepository>();
            var mockMentorRepo = new Mock<IMentorRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts).Returns(mockAccountRepo.Object);
            _mockUnitOfWork.Setup(u => u.Mentors).Returns(mockMentorRepo.Object);
            mockAccountRepo.Setup(u => u.GetByIdMentor(accountId)).ReturnsAsync(account);

            // Act
            await _service.SubmitMentorProfileAsync(accountId, request);

            // Assert
            account.Mentor.Bio.Should().Be("Updated Bio");
            account.Mentor.MentorPositions.Should().HaveCount(2);
            mockMentorRepo.Verify(u => u.Update(account.Mentor), Times.Once);
        }

        [Fact]
        public async Task SubmitMentorProfileAsync_ShouldThrowBadRequest_WhenRequestIsNull()
        {
            // Act
            var act = () => _service.SubmitMentorProfileAsync(1, null!);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Dữ liệu hồ sơ Mentor không hợp lệ.");
        }

        [Fact]
        public async Task SubmitMentorProfileAsync_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.SubmitMentorProfileAsync(1, new UpdateMentorProfileRequest());

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task SubmitMentorProfileAsync_ShouldThrowBadRequest_WhenInvalidBirthdayFormat()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(1)).ReturnsAsync(account);
            var request = new UpdateMentorProfileRequest { BirthDate = "invalid-date" };

            // Act
            var act = () => _service.SubmitMentorProfileAsync(1, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Định dạng ngày sinh không hợp lệ. Vui lòng sử dụng định dạng yyyy-MM-dd.");
        }

        [Fact]
        public async Task SubmitMentorProfileAsync_ShouldThrowBadRequest_WhenBirthdayIsInFuture()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(1)).ReturnsAsync(account);
            var request = new UpdateMentorProfileRequest { BirthDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd") };

            // Act
            var act = () => _service.SubmitMentorProfileAsync(1, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Ngày sinh không được ở trong tương lai.");
        }

        #region View Mentor Details (Additional)
        [Fact]
        public async Task GetAccountDetailMentor_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.GetAccountDetailMentor(1);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetAccountDetailMentor_ShouldThrowBadRequest_WhenNotAMentor()
        {
            // Arrange
            var account = new Account { Id = 1, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(1)).ReturnsAsync(account);

            // Act
            var act = () => _service.GetAccountDetailMentor(1);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }
        #endregion
    }
}
