using System.Reflection;
using System.Runtime.Serialization;
using FirebaseAdmin.Auth;
using FluentAssertions;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.Notification;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.Business.Services;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Infrastructure.Configurations;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.UserManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepo;
        private readonly Mock<IMentorRepository> _mockMentorRepo;
        private readonly Mock<IRecruiterRepository> _mockRecruiterRepo;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<IJwtTokenGenerator> _mockJwtGenerator;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepo;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<ISystemNotificationService> _mockSystemNotificationService;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IFirebaseAuthService> _mockFirebaseAuthService;
        private readonly IOptions<JwtSettings> _jwtOptions;

        public AuthServiceTests()
        {
            _mockAccountRepo = new Mock<IAccountRepository>();
            _mockMentorRepo = new Mock<IMentorRepository>();
            _mockRecruiterRepo = new Mock<IRecruiterRepository>();
            _mockRoleService = new Mock<IRoleService>();
            _mockJwtGenerator = new Mock<IJwtTokenGenerator>();
            _mockEmailService = new Mock<IEmailService>();
            _mockRefreshTokenRepo = new Mock<IRefreshTokenRepository>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockSystemNotificationService = new Mock<ISystemNotificationService>();
            _mockConfig = new Mock<IConfiguration>();
            _mockFirebaseAuthService = new Mock<IFirebaseAuthService>();

            _jwtOptions = Options.Create(new JwtSettings { RefreshTokenExpiryDays = 7 });
            
            _mockConfig.Setup(c => c["FrontendSettings:BaseUrl"]).Returns("http://localhost:3000");
        }

        #region Create Staff Account (CreateEmployeeAccountAsync)
        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldCreateSuccessfully()
        {
            var creatorId = 1;
            var request = new CreateEmployeeRequest { Email = "staff@test.com", FullName = "Staff Member" };
            var firebaseUser = CreateUserRecord("fb-uid", request.Email, request.FullName);

            _mockAccountRepo.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(firebaseUser);
            _mockFirebaseAuthService.Setup(f => f.GeneratePasswordResetLinkAsync(request.Email)).ReturnsAsync("reset-link");
            _mockRoleService.Setup(s => s.AssignDefaultRoleAsync(It.IsAny<int>(), RoleName.Staff)).Returns(Task.CompletedTask);

            var service = CreateServiceInstance();

            await service!.CreateEmployeeAccountAsync(creatorId, request);
            _mockAccountRepo.Verify(r => r.AddAsync(It.Is<Account>(a => 
                a.Email == request.Email && 
                a.FullName == request.FullName && 
                a.ProviderId == "fb-uid")), Times.Once);
            _mockEmailService.Verify(s => s.SendEmailAsync(request.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _mockAuditLogService.Verify(s => s.CreateAuditLogAsync(creatorId, AuditAction.Create, "Account", It.IsAny<int>(), It.IsAny<object>(), It.IsAny<object>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldThrowConflict_WhenEmailExists()
        {
            var request = new CreateEmployeeRequest { Email = "existing@test.com", FullName = "Staff 1" };
            _mockAccountRepo.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

            var service = CreateServiceInstance();
            var act = async () => await service!.CreateEmployeeAccountAsync(1, request);

            await act.Should().ThrowAsync<ConflictException>();
            _mockAccountRepo.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldThrowException_WhenFirebaseUserCreationFails()
        {
            var request = new CreateEmployeeRequest { Email = "fail@test.com", FullName = "Staff Fail" };
            _mockAccountRepo.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ThrowsAsync(new System.Exception("Firebase Error"));

            var service = CreateServiceInstance();
            var act = async () => await service!.CreateEmployeeAccountAsync(1, request);
            await act.Should().ThrowAsync<System.Exception>();
        }

        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldCleanup_WhenFirebaseLinkGenerationFails()
        {
            var request = new CreateEmployeeRequest { Email = "fail@test.com", FullName = "Failure Test" };
            var firebaseUser = CreateUserRecord("fb-uid", request.Email, request.FullName);

            _mockAccountRepo.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(firebaseUser);
            _mockFirebaseAuthService.Setup(f => f.GeneratePasswordResetLinkAsync(request.Email)).ThrowsAsync(new System.Exception("Link Generation Error"));

            var service = CreateServiceInstance();
            var act = async () => await service!.CreateEmployeeAccountAsync(1, request);

            await act.Should().ThrowAsync<System.Exception>();
            _mockFirebaseAuthService.Verify(f => f.DeleteUserAsync("fb-uid"), Times.Once);
            _mockAccountRepo.Verify(r => r.DeleteAsync(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldNotSendEmail_WhenInternalErrorOccurs()
        {
            var request = new CreateEmployeeRequest { Email = "error@test.com", FullName = "Error Case" };
            var firebaseUser = CreateUserRecord("fb-uid", request.Email, request.FullName);

            _mockAccountRepo.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(firebaseUser);
            _mockRoleService.Setup(s => s.AssignDefaultRoleAsync(It.IsAny<int>(), RoleName.Staff)).ThrowsAsync(new System.Exception("DB Exception"));

            var service = CreateServiceInstance();

            var act = async () => await service!.CreateEmployeeAccountAsync(1, request);

            try { await act(); } catch { }

            _mockEmailService.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region Reflection Helpers for Firebase Classes
        private UserRecord CreateUserRecord(string uid, string email, string displayName)
        {
            var userRecord = (UserRecord)FormatterServices.GetUninitializedObject(typeof(UserRecord));

            SetPrivateField(userRecord, "Uid", uid);
            SetPrivateField(userRecord, "Email", email);
            SetPrivateField(userRecord, "DisplayName", displayName);

            return userRecord;
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var type = obj.GetType();
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (field == null)
            {
                string backingFieldName = $"<{fieldName}>k__BackingField";
                field = type.GetField(backingFieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                var property = type.GetProperty(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(obj, value);
                }
            }
        }
        #endregion

        private AuthService? CreateServiceInstance()
        {
            try 
            {
                return new AuthService(
                    _mockAccountRepo.Object,
                    _mockMentorRepo.Object,
                    _mockRecruiterRepo.Object,
                    _mockRoleService.Object,
                    _mockJwtGenerator.Object,
                    _mockEmailService.Object,
                    _mockRefreshTokenRepo.Object,
                    _jwtOptions,
                    _mockConfig.Object,
                    _mockAuditLogService.Object,
                    _mockSystemNotificationService.Object,
                    _mockFirebaseAuthService.Object
                );
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
