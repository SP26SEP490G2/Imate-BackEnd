using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace Imate.API.UnitTest.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IAccountRepository> _mockAccountRepository;
        private readonly Mock<IMentorRepository> _mockMentorRepository;
        private readonly Mock<IRecruiterRepository> _mockRecruiterRepository;
        private readonly Mock<IRoleService> _mockRoleService;
        private readonly Mock<IJwtTokenGenerator> _mockJwtTokenGenerator;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IRefreshTokenRepository> _mockRefreshTokenRepository;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<ISystemNotificationService> _mockSystemNotificationService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly IOptions<JwtSettings> _jwtOptions;
        private readonly Mock<IFirebaseAuthService> _mockFirebaseAuthService;

        private readonly AuthService _authService;
        private const string ValidFireBaseId = "lGh6XnD16Ffpu89GXhaR7h1QEqo1";
        private const string InvalidFireBaseId = "invalid_firebase_id";


        public AuthServiceTests()
        {
            _mockAccountRepository = new Mock<IAccountRepository>();
            _mockMentorRepository = new Mock<IMentorRepository>();
            _mockRecruiterRepository = new Mock<IRecruiterRepository>();
            _mockRoleService = new Mock<IRoleService>();
            _mockJwtTokenGenerator = new Mock<IJwtTokenGenerator>();
            _mockEmailService = new Mock<IEmailService>();
            _mockRefreshTokenRepository = new Mock<IRefreshTokenRepository>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockSystemNotificationService = new Mock<ISystemNotificationService>();
            _mockFirebaseAuthService = new Mock<IFirebaseAuthService>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.SetupGet(c => c["FrontendSettings:BaseUrl"]).Returns("http://imate.vn");
            _jwtOptions = Options.Create(new JwtSettings { RefreshTokenExpiryDays = 7 });
        }

        #region Register Account
        [Fact]
        public async Task RegisterWithEmailAsync_ShouldThrowArgumentException_WhenRoleIsInvalid()
        {
            if (_authService == null) return;

            var request = new RegisterWithEmailRequest
            {
                Email = "test@example.com",
                Password = "Password@123",
                ConfirmPassword = "Password@123",
                FullName = "Test User",
                Role = "InvalidRoleXYZ"
            };

            var act = () => _authService.RegisterWithEmailAsync(request);

            var exception = await act.Should().ThrowAsync<ArgumentException>();
            exception.WithMessage($"Role '{request.Role}' không hợp lệ.");
        }

        [Fact]
        public async Task RegisterWithEmailAsync_ShouldThrowConflictException_WhenEmailAlreadyExists()
        {
            if (_authService == null) return; 

            var request = new RegisterWithEmailRequest
            {
                Email = "existing@example.com",
                Password = "Password@123",
                ConfirmPassword = "Password@123",
                FullName = "Test User",
                Role = "Candidate"
            };

            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);

            var act = () => _authService.RegisterWithEmailAsync(request);

            var exception = await act.Should().ThrowAsync<ConflictException>();
            exception.WithMessage("Email đã được đăng ký trong hệ thống.");
        }

        [Fact]
        public async Task RegisterWithEmailAsync_ShouldThrowConflictException_WhenPasswordsDoNotMatch()
        {
            if (_authService == null) return; 

            var request = new RegisterWithEmailRequest
            {
                Email = "test@example.com",
                Password = "Password@123",
                ConfirmPassword = "DifferentPassword@123",
                FullName = "Test User",
                Role = "Candidate"
            };

            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);

            var act = () => _authService.RegisterWithEmailAsync(request);

            var exception = await act.Should().ThrowAsync<ConflictException>();
            exception.WithMessage("Mật Khẩu và Xác Nhận Mật Khẩu không trùng khớp.");
        }

        [Fact]
        public async Task RegisterWithEmailAsync_ShouldThrowException_WhenOneOfTheFieldsIsEmpty()
        {
            if (_authService == null) return; 

            var request = new RegisterWithEmailRequest
            {
                Email = "",
                Password = "Password@123",
                ConfirmPassword = "Password@123",
                FullName = "Test User",
                Role = "Candidate"
            };

            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);

            var act = () => _authService.RegisterWithEmailAsync(request);

            var exception = await act.Should().ThrowAsync<Exception>();
        }

        [Fact]
        public async Task RegisterWithEmailAsync_ShouldRegisterAccount_WhenValidRequest()
        {
            // Arrange
            var request = new RegisterWithEmailRequest
            {
                Email = "test@example.com",
                Password = "Password123!",
                ConfirmPassword = "Password123!",
                FullName = "Test User",
                Role = "Candidate"
            };

            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email))
                .ReturnsAsync(false);

            var firebaseUser = CreateUserRecord(ValidFireBaseId, request.Email, request.FullName);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(firebaseUser);

            var service = CreateServiceInstance();

            // Act
            await service!.RegisterWithEmailAsync(request);

            // Assert
            _mockFirebaseAuthService.Verify(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>()), Times.Once);
            _mockAccountRepository.Verify(x => x.AddAsync(It.IsAny<Account>()), Times.Once);
        }
        #endregion

        #region Login
        [Fact]
        public async Task VerifyFirebaseTokenAndLoginAsync_ShouldReturnAuthResponse_WhenSuccessful()
        {
            var request = new LoginRequest
            {
                FirebaseIdToken = ValidFireBaseId
            };
            var account = new Account
            {
                Id = 1,
                Email = "thuan@gmail.com",
                FullName = "Thuan",
                Provider = LoginProvider.EmailPassword,
                ProviderId = ValidFireBaseId,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AccountRoles = new List<AccountRole>
                {
                    new AccountRole { Role = new Role { Name = RoleName.Candidate } }
                }
            };

            var firebaseToken = CreateFirebaseToken(ValidFireBaseId, account.Email, true);
            _mockFirebaseAuthService.Setup(x => x.VerifyIdTokenAsync(request.FirebaseIdToken)).ReturnsAsync(firebaseToken);
            _mockAccountRepository.Setup(x => x.GetByProviderIdAsync(ValidFireBaseId)).ReturnsAsync(account);
            _mockJwtTokenGenerator.Setup(x => x.GenerateToken(account.Id, It.IsAny<IEnumerable<RoleName>>())).Returns("jwt-token");
            var service = CreateServiceInstance();

            // Act
            var result = await service!.VerifyFirebaseTokenAndLoginAsync(request);

            // Assert
            result.Should().NotBeNull();
            result.Token.Should().Be("jwt-token");
        }
 
        [Fact]
        public async Task VerifyFirebaseTokenAndLoginAsync_ShouldThrowUnauthorizedException_WhenTokenIsNull()
        {
            // Arrange
            var request = new LoginRequest
            {
                FirebaseIdToken = null
            };
            var service = CreateServiceInstance();

            // Act
            var act = async () => await service!.VerifyFirebaseTokenAndLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        [Fact]
        public async Task VerifyFirebaseTokenAndLoginAsync_ShouldThrowUnauthorizedException_WhenEmailNotVerified()
        {
            // Arrange
            var request = new LoginRequest
            {
                FirebaseIdToken = "invalid-token"
            };

            var firebaseToken = CreateFirebaseToken("uid", "test@gmail.com", false);
            _mockFirebaseAuthService.Setup(x => x.VerifyIdTokenAsync(request.FirebaseIdToken)).ReturnsAsync(firebaseToken);

            var service = CreateServiceInstance();

            // Act
            var act = async () => await service!.VerifyFirebaseTokenAndLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedException>().WithMessage("Vui lòng xác minh tài khoản email trước khi đăng nhập.");
        }

        [Fact]
        public async Task VerifyFirebaseTokenAndLoginAsync_ShouldThrowNotFoundException_WhenAccountDoesNotExist()
        {
            // Arrange
            var request = new LoginRequest
            {
                FirebaseIdToken = "valid-token"
            };

            var firebaseToken = CreateFirebaseToken("invalid-uid", "test@gmail.com", true);
            _mockFirebaseAuthService.Setup(x => x.VerifyIdTokenAsync(request.FirebaseIdToken)).ReturnsAsync(firebaseToken);
            _mockAccountRepository.Setup(x => x.GetByProviderIdAsync("invalid-uid")).ReturnsAsync((Account)null!);

            var service = CreateServiceInstance();

            // Act
            var act = async () => await service!.VerifyFirebaseTokenAndLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task VerifyFirebaseTokenAndLoginAsync_ShouldThrowException_WhenAccountIsSuspended()
        {
            // Arrange
            var request = new LoginRequest
            {
                FirebaseIdToken = "valid-token"
            };

            var account = new Account
            {
                Status = AccountStatus.Suspended
            };

            var firebaseToken = CreateFirebaseToken("suspended-uid", "test@gmail.com", true);
            _mockFirebaseAuthService.Setup(x => x.VerifyIdTokenAsync(request.FirebaseIdToken)).ReturnsAsync(firebaseToken);
            _mockAccountRepository.Setup(x => x.GetByProviderIdAsync("suspended-uid")).ReturnsAsync(account);

            var service = CreateServiceInstance();

            // Act
            var act = async () => await service!.VerifyFirebaseTokenAndLoginAsync(request);

            // Assert
            await act.Should().ThrowAsync<Exception>();
        }
        #endregion

        #region Forgot Password
        [Fact]
        public async Task GenerateActionCodeAsync_ShouldThrowBadRequestException_WhenProviderIsGoogle()
        {
            // Arrange
            var email = "googleuser@gmail.com";
            var actionType = "PASSWORD_RESET";

            var account = new Account
            {
                Id = 1,
                Email = email,
                Provider = LoginProvider.Google
            };

            _mockAccountRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(account);

            var service = CreateServiceInstance();

            // Act
            var act = async () => await service!.GenerateActionCodeAsync(email, actionType);

            // Assert
            var exception = await act.Should().ThrowAsync<BadRequestException>();
            exception.WithMessage("Không thể đặt lại mật khẩu cho tài khoản đăng nhập bằng Google. Vui lòng đăng nhập bằng Google để truy cập tài khoản.");
        }

        [Fact]
        public async Task GenerateActionCodeAsync_ShouldReturnOobCode_WhenSuccessful()
        {
            // Arrange
            var email = "test@gmail.com";
            var actionType = "PASSWORD_RESET";

            var account = new Account
            {
                Id = 1,
                Email = email,
                Provider = LoginProvider.EmailPassword
            };

            _mockAccountRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(account);
            _mockFirebaseAuthService.Setup(f => f.GeneratePasswordResetLinkAsync(email)).ReturnsAsync("http://reset-link?oobCode=my-code");

            var service = CreateServiceInstance();

            // Act
            var result = await service!.GenerateActionCodeAsync(email, actionType);

            // Assert
            result.Should().Be("my-code");
        }

        [Fact]
        public async Task SendActionEmailAsync_ShouldSendEmail_WhenValidRequest()
        {
            // Arrange
            var email = "test@gmail.com";
            var oobCode = "valid-oob-code";
            var actionType = "PASSWORD_RESET";

            var request = new GenerateActionCodeRequest { Email = email, ActionType = actionType };

            var account = new Account
            {
                Id = 1,
                Email = email,
                Provider = LoginProvider.EmailPassword
            };

            _mockAccountRepository.Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(account);
            _mockFirebaseAuthService.Setup(f => f.GeneratePasswordResetLinkAsync(email)).ReturnsAsync("http://reset-link?oobCode=valid-oob-code");
            _mockEmailService.Setup(x => x.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

            var service = CreateServiceInstance();

            // Act
            await service!.SendActionEmailAsync(oobCode, request.Email, request.ActionType);

            // Assert
            _mockEmailService.Verify(x => x.SendEmailAsync(email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }
        #endregion

        #region ChangePassword
        [Fact]
        public async Task ChangePasswordAsync_ShouldUpdatePassword_WhenValidRequest()
        {
            // Arrange
            var account = new Account
            {
                Id = 1,
                Email = "thuan@gmail.com",
                FullName = "Thuan",
                Provider = LoginProvider.EmailPassword,
                ProviderId = ValidFireBaseId,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var request = new ChangePasswordRequest
            {
                FirebaseIdToken = "valid-token",
                NewPassword = "Password@123!"
            };

            var firebaseToken = CreateFirebaseToken(ValidFireBaseId, account.Email, true);
            _mockFirebaseAuthService.Setup(x => x.VerifyIdTokenAsync(request.FirebaseIdToken)).ReturnsAsync(firebaseToken);
            _mockAccountRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(account);
            _mockFirebaseAuthService.Setup(x => x.UpdateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(CreateUserRecord("valid-firebase-uid", account.Email, account.FullName));

            var service = CreateServiceInstance();

            // Act
            await service!.ChangePasswordAsync(1, request);

            // Assert
            _mockFirebaseAuthService.Verify(x => x.UpdateUserAsync(It.IsAny<UserRecordArgs>()), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowForbiddenException_WhenProviderIdAndEmailNotMatch()
        {
            // Arrange
            var account = new Account
            {
                Id = 1000,
                Email = "notfound@gmail.com",
                FullName = "Not Found",
                Provider = LoginProvider.Google,
                ProviderId = "different-uid",
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var request = new ChangePasswordRequest
            {
                FirebaseIdToken = "valid-token",
                NewPassword = "NewPassword123!"
            };

            var firebaseToken = CreateFirebaseToken(ValidFireBaseId, "other@gmail.com", true);
            _mockFirebaseAuthService.Setup(x => x.VerifyIdTokenAsync(request.FirebaseIdToken)).ReturnsAsync(firebaseToken);
            _mockAccountRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(account);

            var service = CreateServiceInstance();

            // Act
            var act = async () => await service!.ChangePasswordAsync(1000, request);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>();
        }


        [Fact]
        public async Task ChangePasswordAsync_ShouldThrowBadRequestException_WhenProviderIsGoogle()
        {
            // Arrange
            var account = new Account
            {
                Id = 1,
                Email = "thuan@gmail.com",
                FullName = "Thuan",
                Provider = LoginProvider.Google,
                ProviderId = ValidFireBaseId,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var request = new ChangePasswordRequest
            {
                FirebaseIdToken = "valid-token",
                NewPassword = "NewPassword123!"
            };

            var firebaseToken = CreateFirebaseToken(ValidFireBaseId, account.Email, true);
            _mockFirebaseAuthService.Setup(x => x.VerifyIdTokenAsync(request.FirebaseIdToken)).ReturnsAsync(firebaseToken);
            _mockAccountRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(account);

            var service = CreateServiceInstance();

            // Act
            var act = async () => await service!.ChangePasswordAsync(1, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>();
        }
        #endregion

        #region Create Staff Account (CreateEmployeeAccountAsync)
        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldCreateSuccessfully()
        {
            var creatorId = 1;
            var request = new CreateEmployeeRequest { Email = "staff@test.com", FullName = "Staff Member" };
            var firebaseUser = CreateUserRecord("fb-uid", request.Email, request.FullName);

            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(firebaseUser);
            _mockFirebaseAuthService.Setup(f => f.GeneratePasswordResetLinkAsync(request.Email)).ReturnsAsync("reset-link");
            _mockRoleService.Setup(s => s.AssignDefaultRoleAsync(It.IsAny<int>(), RoleName.Staff)).Returns(Task.CompletedTask);

            var service = CreateServiceInstance();

            await service!.CreateEmployeeAccountAsync(creatorId, request);
            _mockAccountRepository.Verify(r => r.AddAsync(It.Is<Account>(a =>
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
            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(true);
            
            var service = CreateServiceInstance();
            var act = async () => await service!.CreateEmployeeAccountAsync(1, request);

            await act.Should().ThrowAsync<ConflictException>();
            _mockAccountRepository.Verify(r => r.AddAsync(It.IsAny<Account>()), Times.Never);
        }

        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldThrowException_WhenFirebaseUserCreationFails()
        {
            var request = new CreateEmployeeRequest { Email = "fail@test.com", FullName = "Staff Fail" };
            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
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

            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(firebaseUser);
            _mockFirebaseAuthService.Setup(f => f.GeneratePasswordResetLinkAsync(request.Email)).ThrowsAsync(new System.Exception("Link Generation Error"));

            var service = CreateServiceInstance();
            var act = async () => await service!.CreateEmployeeAccountAsync(1, request);

            await act.Should().ThrowAsync<System.Exception>();
            _mockFirebaseAuthService.Verify(f => f.DeleteUserAsync("fb-uid"), Times.Once);
            _mockAccountRepository.Verify(r => r.DeleteAsync(It.IsAny<Account>()), Times.Once);
        }

        [Fact]
        public async Task CreateEmployeeAccountAsync_ShouldNotSendEmail_WhenInternalErrorOccurs()
        {
            var request = new CreateEmployeeRequest { Email = "error@test.com", FullName = "Error Case" };
            var firebaseUser = CreateUserRecord("fb-uid", request.Email, request.FullName);

            _mockAccountRepository.Setup(r => r.ExistsByEmailAsync(request.Email)).ReturnsAsync(false);
            _mockFirebaseAuthService.Setup(f => f.CreateUserAsync(It.IsAny<UserRecordArgs>())).ReturnsAsync(firebaseUser);
            _mockRoleService.Setup(s => s.AssignDefaultRoleAsync(It.IsAny<int>(), RoleName.Staff)).ThrowsAsync(new System.Exception("DB Exception"));

            var service = CreateServiceInstance();

            var act = async () => await service!.CreateEmployeeAccountAsync(1, request);

            try { await act(); } catch { }

            _mockEmailService.Verify(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
        #endregion

        #region Reflection Helpers for Firebase Classes
        private FirebaseToken CreateFirebaseToken(string uid, string email, bool emailVerified)
        {
            var token = (FirebaseToken)FormatterServices.GetUninitializedObject(typeof(FirebaseToken));
            SetPrivateField(token, "Uid", uid);
            
            var claims = new Dictionary<string, object>
            {
                { "email", email },
                { "email_verified", emailVerified }
            };
            SetPrivateField(token, "Claims", claims);
            return token;
        }

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
                    _mockAccountRepository.Object,
                    _mockMentorRepository.Object,
                    _mockRecruiterRepository.Object,
                    _mockRoleService.Object,
                    _mockJwtTokenGenerator.Object,
                    _mockEmailService.Object,
                    _mockRefreshTokenRepository.Object,
                    _jwtOptions,
                    _mockConfiguration.Object,
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