using FluentAssertions;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.Business.Services.UserManagement;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.Mentors;
using Imate.API.Presentation.ResponseModels.UserManagement;
using MockQueryable;
using Moq;
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

        #region View Profile
        [Fact]
        public async Task GetUserProfileAsync_ShouldThrowNotFoundException_WhenAccountIsNull()
        {
            // Arrange
            _mockAccountRepo.Setup(repo => repo.GetByIdWithDetailsAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);

            // Act
            var act = () => _service.GetUserProfileAsync(1, "Free");

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Không tìm thấy tài khoản người dùng.");
        }

        [Fact]
        public async Task GetUserProfileAsync_ShouldThrowException_WhenUserHasNoRole()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole>() // Không có role
            };
            _mockAccountRepo.Setup(repo => repo.GetByIdWithDetailsAsync(accountId)).ReturnsAsync(account);

            // Act
            var act = () => _service.GetUserProfileAsync(accountId, "Free");

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("Người dùng không có vai trò hợp lệ.");
        }

        [Fact]
        public async Task GetUserProfileAsync_ShouldReturnMentorProfileResponse_WhenRoleIsMentor()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                Email = "mentor@test.com",
                FullName = "Mentor Test",
                AvatarUrl = "http://test.com/avatar.jpg",
                Balance = 1000,
                Status = AccountStatus.Active,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } },
                Mentor = new Mentor
                {
                    Bio = "Mentor Bio",
                    Phone = "123456789",
                    PricePerSession = 50000,
                    AvgRatings = 4.5m,
                    TotalRatingCount = 10,
                    BankAccountHolderName = "Mentor",
                    BankAccountNumber = "2004",
                    BankCode = "OCB",
                    VerificationStatus = VerificationStatus.Verified,
                    MentorSkills = new List<MentorSkill> { new() { Skill = new Skill { Name = "C#" } } },
                    MentorPositions = new List<MentorPosition> { new() { Position = new Position { Name = "Developer" } } },
                    MentorCompanies = new List<MentorCompany> { new() { Company = new Company { Name = "FPT" } } }
                }
            };

            _mockAccountRepo.Setup(repo => repo.GetByIdWithDetailsAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserProfileAsync(accountId, "Free");

            // Assert
            result.Should().BeOfType<MentorProfileResponse>();
            var mentorResponse = result as MentorProfileResponse;
            mentorResponse.Should().NotBeNull();
            mentorResponse!.Id.Should().Be(accountId);
            mentorResponse.Role.Should().Be(RoleName.Mentor.ToString());
            mentorResponse.Bio.Should().Be("Mentor Bio");
            mentorResponse.Skills.Should().Contain("C#");
            mentorResponse.Positions.Should().Contain("Developer");
            mentorResponse.Companies.Should().Contain("FPT");
        }

        [Fact]
        public async Task GetUserProfileAsync_ShouldReturnRecruiterProfileResponse_WhenRoleIsRecruiter()
        {
            // Arrange
            var accountId = 2;
            var account = new Account
            {
                Id = accountId,
                Email = "recruiter@test.com",
                FullName = "Recruiter Test",
                Status = AccountStatus.Active,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } },
                Recruiter = new Recruiter
                {
                    CompanyName = "Imate",
                    Industry = "IT",
                    VerificationStatus = VerificationStatus.Verified
                }
            };

            _mockAccountRepo.Setup(repo => repo.GetByIdWithDetailsAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserProfileAsync(accountId, "Free");

            // Assert
            result.Should().BeOfType<RecruiterProfileResponse>();
            var recruiterResponse = result as RecruiterProfileResponse;
            recruiterResponse.Should().NotBeNull();
            recruiterResponse!.Id.Should().Be(accountId);
            recruiterResponse.Role.Should().Be(RoleName.Recruiter.ToString());
            recruiterResponse.CompanyName.Should().Be("Imate");
            recruiterResponse.Industry.Should().Be("IT");
        }

        [Fact]
        public async Task GetUserProfileAsync_ShouldReturnUserProfileResponse_WhenRoleIsMentorButNoMentorData()
        {
            // Arrange
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                Email = "mentor@test.com",
                Status = AccountStatus.Active,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } },
                Mentor = null // Null
            };

            _mockAccountRepo.Setup(repo => repo.GetByIdWithDetailsAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserProfileAsync(accountId, "Premium");

            // Assert
            result.Should().BeOfType<UserProfileResponse>();
            result.Role.Should().Be(RoleName.Mentor.ToString());
            (result as UserProfileResponse)!.Subscription.Should().Be("Premium");
        }

        [Fact]
        public async Task GetUserProfileAsync_ShouldReturnUserProfileResponse_WhenRoleIsCandidate()
        {
            // Arrange
            var accountId = 3;
            var account = new Account
            {
                Id = accountId,
                Email = "candidate@test.com",
                FullName = "Candidate Test",
                Status = AccountStatus.Active,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } }
            };

            _mockAccountRepo.Setup(repo => repo.GetByIdWithDetailsAsync(accountId)).ReturnsAsync(account);

            // Act
            var result = await _service.GetUserProfileAsync(accountId, "Free");

            // Assert
            result.Should().BeOfType<UserProfileResponse>();
            result.Should().NotBeOfType<MentorProfileResponse>();
            result.Should().NotBeOfType<RecruiterProfileResponse>();
            result.Id.Should().Be(accountId);
            result.Role.Should().Be(RoleName.Candidate.ToString());
            (result as UserProfileResponse)!.Subscription.Should().Be("Free");
        }
        #endregion

        #region Update Profile
        [Fact]
        public async Task UpdateGeneralProfileAsync_ShouldThrowNotFoundException_WhenAccountDoesNotExist()
        {
            // Arrange
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(1)).ReturnsAsync((Account?)null);

            var request = new UpdateGeneralProfileRequest { FullName = "New Name" };

            // Act
            var act = () => _service.UpdateGeneralProfileAsync(1, request);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Không tìm thấy tài khoản.");
        }

        [Fact]
        public async Task UpdateGeneralProfileAsync_ShouldThrowBadRequestException_WhenAvatarFileIsEmpty()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            var request = new UpdateGeneralProfileRequest { AvatarFile = mockFile.Object };

            // Act
            var act = () => _service.UpdateGeneralProfileAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("File ảnh không được rỗng.");
        }

        [Fact]
        public async Task UpdateGeneralProfileAsync_ShouldThrowBadRequestException_WhenContentTypeIsInvalid()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);
            mockFile.Setup(f => f.FileName).Returns("file.txt");
            mockFile.Setup(f => f.ContentType).Returns("text/plain");

            var request = new UpdateGeneralProfileRequest { AvatarFile = mockFile.Object };

            // Act
            var act = () => _service.UpdateGeneralProfileAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("File không hợp lệ. Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif, .webp.");
        }

        [Fact]
        public async Task UpdateGeneralProfileAsync_ShouldThrowBadRequestException_WhenExtensionIsInvalid()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(100);
            mockFile.Setup(f => f.FileName).Returns("image.pdf");
            mockFile.Setup(f => f.ContentType).Returns("image/pdf");

            var request = new UpdateGeneralProfileRequest { AvatarFile = mockFile.Object };

            // Act
            var act = () => _service.UpdateGeneralProfileAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("File không hợp lệ. Chỉ chấp nhận các định dạng ảnh: .jpg, .jpeg, .png, .gif, .webp.");
        }

        [Fact]
        public async Task UpdateGeneralProfileAsync_ShouldThrowBadRequestException_WhenFileSizeExceedsLimit()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            mockFile.Setup(f => f.FileName).Returns("image.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var request = new UpdateGeneralProfileRequest { AvatarFile = mockFile.Object };

            // Act
            var act = () => _service.UpdateGeneralProfileAsync(accountId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Dung lượng ảnh quá lớn. Vui lòng tải lên ảnh nhỏ hơn 5MB.");
        }

        [Fact]
        public async Task UpdateGeneralProfileAsync_ShouldUpdateOnlyFullName_WhenAvatarFileIsNull()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId, FullName = "Old Name" };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);

            var request = new UpdateGeneralProfileRequest { FullName = "New Name", AvatarFile = null };

            // Act
            await _service.UpdateGeneralProfileAsync(accountId, request);

            // Assert
            account.FullName.Should().Be("New Name");
            _mockUnitOfWork.Verify(u => u.Accounts.UpdateAsync(account), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            _mockS3Service.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
            _mockS3Service.Verify(s => s.UploadFileAsync(It.IsAny<Microsoft.AspNetCore.Http.IFormFile>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdateGeneralProfileAsync_ShouldUpdateSucessfully_WhenFullNameAndAvatarHasBeenChanged()
        {
            // Arrange
            var accountId = 1;
            var account = new Account { Id = accountId, FullName = "Old Name", AvatarUrl = "old_avatar_url" };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("image.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            _mockS3Service.Setup(s => s.UploadFileAsync(mockFile.Object, "avatars")).ReturnsAsync("new_avatar_url");

            var request = new UpdateGeneralProfileRequest { FullName = "New Name", AvatarFile = mockFile.Object };

            // Act
            await _service.UpdateGeneralProfileAsync(accountId, request);

            // Assert
            account.FullName.Should().Be("New Name");
            account.AvatarUrl.Should().Be("new_avatar_url");
            _mockS3Service.Verify(s => s.DeleteFileAsync("old_avatar_url"), Times.Once);
            _mockS3Service.Verify(s => s.UploadFileAsync(mockFile.Object, "avatars"), Times.Once);
            _mockUnitOfWork.Verify(u => u.Accounts.UpdateAsync(account), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMentorProfileAsync_ShouldThrowNotFound_WhenMentorDoesNotExist()
        {
            var mentorService = new Imate.API.Business.Services.Mentors.MentorService(_mockUnitOfWork.Object);
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(1)).ReturnsAsync((Mentor?)null);

            var act = () => mentorService.UpdateMentorProfileAsync(1, new UpdateMentorProfileRequest());

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Không tìm thấy hồ sơ Mentor.");
        }

        [Fact]
        public async Task UpdateMentorProfileAsync_ShouldThrowBadRequest_WhenBirthDateIsInFuture()
        {
            var mentorService = new Imate.API.Business.Services.Mentors.MentorService(_mockUnitOfWork.Object);
            var mentor = new Mentor { AccountId = 1, PricePerSession = 100 };
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(1)).ReturnsAsync(mentor);
            var request = new UpdateMentorProfileRequest
            {
                PricePerSession = 100,
                BirthDate = DateTime.UtcNow.AddDays(5).ToString("yyyy-MM-dd")
            };

            var act = () => mentorService.UpdateMentorProfileAsync(1, request);

            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Ngày sinh không được ở trong tương lai.");
        }

        [Fact]
        public async Task UpdateMentorProfileAsync_ShouldThrowBadRequest_WhenBirthDateIsInvalidFormat()
        {
            var mentorService = new Imate.API.Business.Services.Mentors.MentorService(_mockUnitOfWork.Object);
            var mentor = new Mentor { AccountId = 1, PricePerSession = 100 };
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(1)).ReturnsAsync(mentor);
            var request = new UpdateMentorProfileRequest
            {
                PricePerSession = 100,
                BirthDate = "invalid-date"
            };

            var act = () => mentorService.UpdateMentorProfileAsync(1, request);

            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Định dạng ngày sinh không hợp lệ. Vui lòng sử dụng định dạng yyyy-MM-dd.");
        }

        [Fact]
        public async Task UpdateMentorProfileAsync_ShouldUpdateProfileAndPriceSuccessfully_WhenPriceChanges()
        {
            var mentorService = new Imate.API.Business.Services.Mentors.MentorService(_mockUnitOfWork.Object);
            var mentor = new Mentor
            {
                AccountId = 1,
                PricePerSession = 100,
                Yoe = 5,
                PriceLastUpdatedDate = null
            };
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(1)).ReturnsAsync(mentor);

            var request = new UpdateMentorProfileRequest
            {
                Bio = "New Bio",
                Phone = "123456789",
                PricePerSession = 200,
                BankAccountHolderName = "Mentor",
                BankAccountNumber = "12345",
                BankCode = "OCB",
                Yoe = 10,
                BirthDate = "2004-10-20"
            };

            await mentorService.UpdateMentorProfileAsync(1, request);

            mentor.PricePerSession.Should().Be(200);
            mentor.PriceLastUpdatedDate.Should().NotBeNull();
            mentor.Bio.Should().Be("New Bio");
            mentor.Yoe.Should().Be(10);
            mentor.BirthDate.Should().Be(new DateOnly(2004, 10, 20));
            _mockUnitOfWork.Verify(u => u.Mentors.UpdateMentorAsync(mentor), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateMentorProfileAsync_ShouldNotUpdatePriceLastUpdatedDate_WhenPriceRemainsUnchanged()
        {
            var mentorService = new Imate.API.Business.Services.Mentors.MentorService(_mockUnitOfWork.Object);
            var mentor = new Mentor { AccountId = 1, PricePerSession = 100, Yoe = 5 };
            _mockUnitOfWork.Setup(u => u.Mentors.GetMentorByIdAsync(1)).ReturnsAsync(mentor);

            var request = new UpdateMentorProfileRequest { PricePerSession = 100, Yoe = null };

            await mentorService.UpdateMentorProfileAsync(1, request);

            mentor.PricePerSession.Should().Be(100);
            mentor.PriceLastUpdatedDate.Should().BeNull();
            mentor.Yoe.Should().Be(5);
            _mockUnitOfWork.Verify(u => u.Mentors.UpdateMentorAsync(mentor), Times.Once);
        }

        [Fact]
        public async Task UpdataRecruiterProfileAsync_ShouldThrowNotFound_WhenRecruiterDoesNotExist()
        {
            var recruiterService = new Imate.API.Business.Services.Recruiters.RecruiterService(
                _mockUnitOfWork.Object, new Mock<IAuditLogService>().Object, new Mock<IEmailService>().Object, _mockS3Service.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetRecruiterByIdAsync(1)).ReturnsAsync((Recruiter?)null);

            var act = () => recruiterService.UpdataRecruiterrProfileAsync(1, new UpdateRecruiterProfileRequest());

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("Không tìm thấy hồ sơ Recruiter.");
        }

        [Fact]
        public async Task UpdataRecruiterProfileAsync_ShouldThrowBadRequest_WhenCompanyLogoIsEmpty()
        {
            var recruiterService = new Imate.API.Business.Services.Recruiters.RecruiterService(
                _mockUnitOfWork.Object, new Mock<IAuditLogService>().Object, new Mock<IEmailService>().Object, _mockS3Service.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetRecruiterByIdAsync(1)).ReturnsAsync(new Recruiter { AccountId = 1 });

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);
            var request = new UpdateRecruiterProfileRequest { CompanyLogo = mockFile.Object };

            var act = () => recruiterService.UpdataRecruiterrProfileAsync(1, request);

            await act.Should().ThrowAsync<BadRequestException>().WithMessage("File ảnh không được rỗng.");
        }

        [Fact]
        public async Task UpdataRecruiterProfileAsync_ShouldUpdateRecruiterAndUploadLogo_WhenValidLogoProvided()
        {
            var recruiterService = new Imate.API.Business.Services.Recruiters.RecruiterService(
                _mockUnitOfWork.Object, new Mock<IAuditLogService>().Object, new Mock<IEmailService>().Object, _mockS3Service.Object);
            var recruiter = new Recruiter { AccountId = 1, CompanyLogo = "old_logo.png" };
            _mockUnitOfWork.Setup(u => u.Recruiters.GetRecruiterByIdAsync(1)).ReturnsAsync(recruiter);

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("new_logo.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            _mockS3Service.Setup(s => s.UploadFileAsync(mockFile.Object, "company-logos")).ReturnsAsync("new_logo_url");

            var request = new UpdateRecruiterProfileRequest { CompanyLogo = mockFile.Object };

            await recruiterService.UpdataRecruiterrProfileAsync(1, request);

            recruiter.CompanyLogo.Should().Be("new_logo_url");
            _mockS3Service.Verify(s => s.DeleteFileAsync("old_logo.png"), Times.Once);
            _mockS3Service.Verify(s => s.UploadFileAsync(mockFile.Object, "company-logos"), Times.Once);
        }

        [Fact]
        public async Task UpdataRecruiterProfileAsync_ShouldNotTouchCompanyLogo_WhenLogoIsNull()
        {
            var recruiterService = new Imate.API.Business.Services.Recruiters.RecruiterService(
                _mockUnitOfWork.Object, new Mock<IAuditLogService>().Object, new Mock<IEmailService>().Object, _mockS3Service.Object);
            var recruiter = new Recruiter { AccountId = 1, CompanyLogo = "old_logo.png" };
            _mockUnitOfWork.Setup(u => u.Recruiters.GetRecruiterByIdAsync(1)).ReturnsAsync(recruiter);

            var request = new UpdateRecruiterProfileRequest { CompanyLogo = null };

            await recruiterService.UpdataRecruiterrProfileAsync(1, request);

            recruiter.CompanyLogo.Should().Be("old_logo.png");
            _mockS3Service.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpdataRecruiterrProfileAsync_ShouldThrowBadRequest_WhenContentTypeOrExtensionIsInvalid()
        {
            var recruiterService = new Imate.API.Business.Services.Recruiters.RecruiterService(
                _mockUnitOfWork.Object, new Mock<IAuditLogService>().Object, new Mock<IEmailService>().Object, _mockS3Service.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetRecruiterByIdAsync(1)).ReturnsAsync(new Recruiter { AccountId = 1 });

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(1024);
            mockFile.Setup(f => f.FileName).Returns("file.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            var request = new UpdateRecruiterProfileRequest { CompanyLogo = mockFile.Object };

            var act = () => recruiterService.UpdataRecruiterrProfileAsync(1, request);

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            await act.Should().ThrowAsync<BadRequestException>().WithMessage($"File không hợp lệ. Chỉ chấp nhận các định dạng ảnh: {string.Join(", ", allowedExtensions)}.");
        }

        [Fact]
        public async Task UpdataRecruiterrProfileAsync_ShouldThrowBadRequest_WhenFileExceedsMaxSize()
        {
            var recruiterService = new Imate.API.Business.Services.Recruiters.RecruiterService(
                _mockUnitOfWork.Object, new Mock<IAuditLogService>().Object, new Mock<IEmailService>().Object, _mockS3Service.Object);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetRecruiterByIdAsync(1)).ReturnsAsync(new Recruiter { AccountId = 1 });

            var mockFile = new Mock<Microsoft.AspNetCore.Http.IFormFile>();
            mockFile.Setup(f => f.Length).Returns(6 * 1024 * 1024); // 6MB
            mockFile.Setup(f => f.FileName).Returns("image.jpg");
            mockFile.Setup(f => f.ContentType).Returns("image/jpeg");

            var request = new UpdateRecruiterProfileRequest { CompanyLogo = mockFile.Object };

            var act = () => recruiterService.UpdataRecruiterrProfileAsync(1, request);

            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Dung lượng ảnh quá lớn. Vui lòng tải lên ảnh nhỏ hơn 5MB.");
        }

        #endregion

        #region View User Account
        [Fact]
        public async Task GetAllAccountAsync_ShouldReturnPagedList_WhenAccountsExist()
        {
            var accountParams = new AccountParams { PageNumber = 1, PageSize = 10 };
            var accountsList = new List<Account>
            {
                new Account
                {
                    Id = 1,
                    FullName = "User 1",
                    Email = "user1@test.com",
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    AccountRoles = new List<AccountRole>
                    {
                        new() { Role = new Role { Name = RoleName.Candidate } }
                    }
                },
                new Account
                {
                    Id = 2,
                    FullName = "User 2",
                    Email = "user2@test.com",
                    Status = AccountStatus.Active,
                    CreatedAt = DateTime.UtcNow,
                    AccountRoles = new List<AccountRole>
                    {
                        new() { Role = new Role { Name = RoleName.Mentor } }
                    }
                }
            };

            var mockQueryable = accountsList.AsQueryable().BuildMock();
            _mockAccountRepo.Setup(r => r.GetAllAccount()).Returns(mockQueryable);

            var result = await _service.GetAllAccountAsync(accountParams);

            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetAllAccountAsync_ShouldFilterBySearchTerm()
        {
            var accountParams = new AccountParams { SearchTerm = "Target", PageNumber = 1, PageSize = 10 };
            var accountsList = new List<Account>
            {
                new Account { Id = 1, FullName = "Target User", AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                new Account { Id = 2, FullName = "Other User", AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } }
            };

            var mockQueryable = accountsList.BuildMock();
            _mockAccountRepo.Setup(r => r.GetAllAccount()).Returns(mockQueryable);
            var result = await _service.GetAllAccountAsync(accountParams);

            result.Items.Should().HaveCount(1);
            result.Items[0].FullName.Should().Be("Target User");
        }

        [Fact]
        public async Task GetAllAccountAsync_ShouldFilterByStatus()
        {
            var accountParams = new AccountParams { AccountStatus = AccountStatus.Suspended, PageNumber = 1, PageSize = 10 };
            var accountsList = new List<Account>
            {
                new Account { Id = 1, Status = AccountStatus.Active, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                new Account { Id = 2, Status = AccountStatus.Suspended, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } }
            };

            var mockQueryable = accountsList.BuildMock();
            _mockAccountRepo.Setup(r => r.GetAllAccount()).Returns(mockQueryable);

            var result = await _service.GetAllAccountAsync(accountParams);

            result.Items.Should().HaveCount(1);
            result.Items[0].Status.Should().Be(AccountStatus.Suspended);
        }

        [Fact]
        public async Task GetAllAccountAsync_ShouldSortByFullNameDesc()
        {
            var accountParams = new AccountParams { SortBy = "fullname", SortOrder = "desc", PageNumber = 1, PageSize = 10 };
            var accountsList = new List<Account>
            {
                new Account { Id = 1, FullName = "A User", AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                new Account { Id = 2, FullName = "Z User", AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } }
            };

            var mockQueryable = accountsList.BuildMock();
            _mockAccountRepo.Setup(r => r.GetAllAccount()).Returns(mockQueryable);

            var result = await _service.GetAllAccountAsync(accountParams);

            result.Items[0].FullName.Should().Be("Z User");
            result.Items[1].FullName.Should().Be("A User");
        }

        [Fact]
        public async Task GetAllAccountAsync_ShouldThrowArgumentException_WhenSortOrderInvalid()
        {
            var accountParams = new AccountParams { SortBy = "fullname", SortOrder = "invalid", PageNumber = 1, PageSize = 10 };
            var mockQueryable = new List<Account>().BuildMock();
            _mockAccountRepo.Setup(r => r.GetAllAccount()).Returns(mockQueryable);

            var act = () => _service.GetAllAccountAsync(accountParams);

            await act.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task GetAllAccountAsync_ShouldThrowNotFound_WhenSortByFieldInvalid()
        {
            var accountParams = new AccountParams { SortBy = "invalidfield", PageNumber = 1, PageSize = 10 };
            var mockQueryable = new List<Account>().BuildMock();
            _mockAccountRepo.Setup(r => r.GetAllAccount()).Returns(mockQueryable);

            var act = () => _service.GetAllAccountAsync(accountParams);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetAllAccountAsync_ShouldTrimSearchTerm_WhenSearchTermHasWhitespace()
        {
            var accountParams = new AccountParams { SearchTerm = "  Target  ", PageNumber = 1, PageSize = 10 };
            var accountsList = new List<Account>
            {
                new Account{ Id = 1, FullName = "Target User", AccountRoles = new List<AccountRole>{new() { Role = new Role { Name = RoleName.Candidate } } } },
                new Account{ Id = 2, FullName = "Other User", AccountRoles = new List<AccountRole>{new() { Role = new Role { Name = RoleName.Candidate } } } }
            };

            var mockQueryable = accountsList.AsQueryable().BuildMock();
            _mockAccountRepo.Setup(r => r.GetAllAccount()).Returns(mockQueryable);

            var result = await _service.GetAllAccountAsync(accountParams);

            result.Items.Should().HaveCount(1);
            result.Items[0].FullName.Should().Be("Target User");
        }

        [Fact]
        public async Task GetAccountOverview_ShouldReturnCorrectSummaryData()
        {
            var now = DateTime.UtcNow;
            var accounts = new List<Account>
                {
                    new Account{ Id = 1, CreatedAt = now.AddDays(-2), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                    new Account{ Id = 2, CreatedAt = now.AddDays(-5), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                    new Account{ Id = 3, CreatedAt = now.AddDays(-10), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                    new Account{ Id = 4, CreatedAt = now.AddDays(-2), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Admin } } } }
                };

            var mockQueryable = accounts.AsQueryable().BuildMock();
            _mockUnitOfWork.Setup(u => u.Accounts.GetAllAccount()).Returns(mockQueryable);

            var result = await _service.GetAccountOverview();

            result.Should().NotBeNull();
            result.TotalUsers.Value.Should().Be(3);
            result.NewUsers.Value.Should().Be(2);
        }

        [Fact]
        public async Task GetAccountOverview_ShouldReturnCorrectChartData()
        {
            var now = DateTime.UtcNow;
            var accounts = new List<Account>
                {
                    new Account{ Id = 1, CreatedAt = now.AddDays(-3), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                    new Account{ Id = 2, CreatedAt = now.AddDays(-10), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                    new Account{ Id = 3, CreatedAt = now.AddDays(-17), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                    new Account{ Id = 4, CreatedAt = now.AddDays(-25), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } },
                    new Account{ Id = 5, CreatedAt = now.AddMonths(-1), AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } }
                };

            var mockQueryable = accounts.AsQueryable().BuildMock();
            _mockUnitOfWork.Setup(u => u.Accounts.GetAllAccount()).Returns(mockQueryable);

            var result = await _service.GetAccountOverview();

            result.TotalUsers.Data.Should().HaveCount(5);
            result.NewUsers.Data.Should().HaveCount(4);
        }
        #endregion

        #region View User Account Details
        [Fact]
        public async Task GetAccountByIdAsync_ShouldReturnAccount_WhenExists()
        {
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                FullName = "User 1",
                Email = "user1@test.com",
                Status = AccountStatus.Active,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } }
            };

            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.GetAccountByIdAsync(accountId);

            result.Should().NotBeNull();
            result.Id.Should().Be(accountId);
            result.FullName.Should().Be("User 1");
        }

        [Fact]
        public async Task GetAccountByIdAsync_ShouldThrowNotFound_WhenDoesNotExist()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);
            var act = () => _service.GetAccountByIdAsync(99);

            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task GetAccountDetailMentorForAdmin_ShouldReturnDetails_WhenValidMentor()
        {
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                FullName = "Mentor Name",
                Email = "mentor@test.com",
                Status = AccountStatus.Active,
                Mentor = new Mentor { Phone = "123", Bio = "Bio", PricePerSession = 500, AvgRatings = 4.5m },
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Mentor } } }
            };

            var mockBookingRepo = new Mock<IBookingRepository>();
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Bookings).Returns(mockBookingRepo.Object);
            mockBookingRepo.Setup(r => r.GetMappedReviewsByMentorIdAsync(accountId)).ReturnsAsync(new List<ReviewResponseModel>());
            mockBookingRepo.Setup(r => r.CountCompletedBookingsByMentorIdAsync(accountId)).ReturnsAsync(5);

            var result = await _service.GetAccountDetailMentor(accountId);

            result.Should().NotBeNull();
            result.FullName.Should().Be("Mentor Name");
            result.TotalCompletedSessions.Should().Be(5);
            result.RoleName.Should().Be("Mentor");
        }

        [Fact]
        public async Task GetAccountDetailMentorForAdmin_ShouldThrowBadRequest_WhenNotAMentor()
        {
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } }
            };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(accountId)).ReturnsAsync(account);

            var act = () => _service.GetAccountDetailMentor(accountId);

            await act.Should().ThrowAsync<BadRequestException>().WithMessage("*không phải là mentor*");
        }

        [Fact]
        public async Task GetAccountDetailStaffForAdmin_ShouldReturnDetails_WhenValidStaff()
        {
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                FullName = "Staff Name",
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Staff } } }
            };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdMentor(accountId)).ReturnsAsync(account);
            
            var mockQuestions = new List<Question>().AsQueryable().BuildMock();
            _mockUnitOfWork.Setup(u => u.Questions.GetAllSystemQuestionsForStaff()).Returns(mockQuestions);

            var result = await _service.GetAccountDetailStaff(accountId);

            result.Should().NotBeNull();
            result.FullName.Should().Be("Staff Name");
            result.RoleName.Should().Be("Staff");
        }

        [Fact]
        public async Task GetAccountDetailCandidateForAdmin_ShouldReturnDetails_WithActivePackage_WhenValidCandidate()
        {
            var accountId = 1;
            var account = new Account
            {
                Id = accountId,
                FullName = "Candidate Name",
                AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } }
            };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);

            var subscriptions = new List<UserSubscription>
            {
                new UserSubscription 
                { 
                    Package = new SubscriptionPackage { Name = "Premium", DurationDays = 30 },
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5))
                }
            };
            _mockUnitOfWork.Setup(u => u.UserSubscriptions.GetSubscriptionsByCandidateIdAsync(accountId)).ReturnsAsync(subscriptions);
            _mockUnitOfWork.Setup(u => u.Bookings.CountBookingsCompletedByCandidateIdAsync(accountId)).ReturnsAsync(3);

            var result = await _service.GetAccountDetailCandidate(accountId);

            result.Should().NotBeNull();
            result.PresentPackage.Should().Be("Premium");
            result.ExPackages.Should().Contain("Premium");
            result.MentorSessionCount.Should().Be(3);
        }

        [Fact]
        public async Task GetAccountDetailCandidateForAdmin_ShouldHandleEmptySubscriptions()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Candidate } } } };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.UserSubscriptions.GetSubscriptionsByCandidateIdAsync(accountId)).ReturnsAsync(new List<UserSubscription>());
            _mockUnitOfWork.Setup(u => u.Bookings.CountBookingsCompletedByCandidateIdAsync(accountId)).ReturnsAsync(0);

            var result = await _service.GetAccountDetailCandidate(accountId);

            result.PresentPackage.Should().BeNull();
            result.ExPackages.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAccountDetailRecruiterForAdmin_ShouldReturnDetails_WhenValidRecruiter()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, FullName = "Recruiter Name", AccountRoles = new List<AccountRole> { new() { Role = new Role { Name = RoleName.Recruiter } } } };
            var recruiter = new Recruiter { AccountId = accountId, CompanyName = "Imate Tech" };
            
            _mockUnitOfWork.Setup(u => u.Accounts.GetByIdAsync(accountId)).ReturnsAsync(account);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetRecruiterByIdAsync(accountId)).ReturnsAsync(recruiter);
            _mockUnitOfWork.Setup(u => u.Recruiters.GetJobsByRecruiterId(accountId)).Returns(new List<Job>().AsQueryable());
            var result = await _service.GetAccountDetailRecruiter(accountId);

            result.Should().NotBeNull();
            result.CompanyName.Should().Be("Imate Tech");
            result.JobPostCount.Should().Be(0);
        }
        #endregion

        #region Update User Status
        [Fact]
        public async Task UpdateAccountStatusAsync_ShouldUpdateStatus_WhenValid()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.UpdateAccountStatusAsync(accountId, "Suspended");

            result.Status.Should().Be(AccountStatus.Suspended);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_ShouldThrowBadRequest_WhenStatusInvalid()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var act = () => _service.UpdateAccountStatusAsync(accountId, "InvalidStatus");
            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_ShouldThrowNotFound_WhenAccountDoesNotExist()
        {
            _mockAccountRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Account?)null);

            var act = () => _service.UpdateAccountStatusAsync(99, "Active");
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_ShouldBeCaseInsensitive()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.UpdateAccountStatusAsync(accountId, "suspended");
            result.Status.Should().Be(AccountStatus.Suspended);
        }

        [Fact]
        public async Task UpdateAccountStatusAsync_ShouldUpdateToSameStatus_AndStillCallRepository()
        {
            var accountId = 1;
            var account = new Account { Id = accountId, Status = AccountStatus.Active };
            _mockAccountRepo.Setup(r => r.GetByIdAsync(accountId)).ReturnsAsync(account);

            var result = await _service.UpdateAccountStatusAsync(accountId, "Active");

            result.Status.Should().Be(AccountStatus.Active);
            _mockAccountRepo.Verify(r => r.UpdateAsync(account), Times.Once);
        }
        #endregion
    }
}
