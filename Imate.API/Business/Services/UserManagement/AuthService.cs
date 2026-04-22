using FirebaseAdmin.Auth;
using Microsoft.Extensions.Options;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Business.Exceptions;
using Imate.API.Infrastructure.Configurations;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.UserManagement;
using System.Text;
using Imate.API.Presentation.ResponseModels.Recruiter;
using Microsoft.Identity.Client;
using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Recruiters;
using Imate.API.Business.Interfaces.Notification;
using System.Security.Principal;

namespace Imate.API.Business.Services
{
    public class AuthService : IAuthService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IMentorRepository _mentorRepository;
        private readonly IRecruiterRepository _recruiterRepository;
        private readonly IRoleService _roleService;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IFirebaseAuthService _firebaseAuth;
        private readonly IEmailService _emailService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly IConfiguration _configuration;
        private readonly string _frontendBaseUrl;
        private readonly IAuditLogService _auditLogService;
        private readonly ISystemNotificationService _systemNotificationService;

        public AuthService(
            IAccountRepository accountRepository,
            IMentorRepository mentorRepository,
            IRecruiterRepository recruiterRepository,
            IRoleService roleService,
            IJwtTokenGenerator jwtTokenGenerator,
            IEmailService emailService,
            IRefreshTokenRepository refreshTokenRepository,
            IOptions<JwtSettings> jwtOptions,
            IConfiguration configuration,
            IAuditLogService auditLogService,
            ISystemNotificationService systemNotificationService,
            IFirebaseAuthService firebaseAuth)
        {
            _accountRepository = accountRepository;
            _mentorRepository = mentorRepository;
            _recruiterRepository = recruiterRepository;
            _roleService = roleService;
            _jwtTokenGenerator = jwtTokenGenerator;
            _firebaseAuth = firebaseAuth;
            _emailService = emailService;
            _refreshTokenRepository = refreshTokenRepository;
            _jwtSettings = jwtOptions.Value;
            _configuration = configuration;
            _frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"] ??
                              throw new ArgumentNullException("FrontendSettings:BaseUrl is not set in appsettings");
            _auditLogService = auditLogService;
            _systemNotificationService = systemNotificationService;
        }

        

        public async Task<AuthResponse> RegisterWithEmailAsync(RegisterWithEmailRequest request)
        {
            // --- 1. VALIDATION ROLE ---
            // Cố gắng parse string sang Enum (ignoreCase: true để không phân biệt hoa thường)
            if (!Enum.TryParse<RoleName>(request.Role, true, out var parsedRole))
            {
                throw new ArgumentException($"Role '{request.Role}' không hợp lệ.");
            }
            // 1. Kiểm tra tính hợp lệ và trùng lặp Email
            if (await _accountRepository.ExistsByEmailAsync(request.Email))
            {
                throw new ConflictException("Email đã được đăng ký trong hệ thống.");
            }

            if (request.Password != request.ConfirmPassword)
            {
                throw new ConflictException("Mật Khẩu và Xác Nhận Mật Khẩu không trùng khớp.");
            }

            UserRecord firebaseUser;    
            try
            {
                // 2. TẠO USER TRÊN FIREBASE AUTH VÀ LẤY UID (PROVIDER ID)
                var args = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = request.Password,
                    DisplayName = request.FullName,
                    EmailVerified = false
                };
                firebaseUser = await _firebaseAuth.CreateUserAsync(args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Đăng ký Firebase thất bại: {ex.Message}");
            }

            var requestedRole = ConvertUserRoleToRoleName(request.Role);

            var accountStatus = (requestedRole == RoleName.Mentor || requestedRole == RoleName.Recruiter)
                ? AccountStatus.PendingVerification
                : AccountStatus.Active;

            var newAccount = new Account
            {
                Email = request.Email,
                FullName = request.FullName,
                Provider = LoginProvider.EmailPassword,
                ProviderId = firebaseUser.Uid,
                Status = accountStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _accountRepository.AddAsync(newAccount);

            await _roleService.AssignDefaultRoleAsync(newAccount.Id, requestedRole);

            // 5. TẠO VÀ TRẢ VỀ LOCAL JWT TOKEN (Sử dụng JWT Generator)
            var roles = await _roleService.GetRolesForAccountAsync(newAccount.Id);
            var token = _jwtTokenGenerator.GenerateToken(newAccount.Id, roles);
            
            // TẠO VÀ LƯU REFRESH TOKEN
            var refreshToken = await CreateAndSaveRefreshTokenAsync(newAccount.Id);

            var rolesList = roles.ToList();
            var primaryRole = rolesList.FirstOrDefault().ToString() ?? "Candidate";
            var verificationStatus = await GetVerificationStatusAsync(newAccount.Id, primaryRole);

            var userDto = new UserDto
            {
                Id = newAccount.Id,
                FullName = newAccount.FullName,
                Email = newAccount.Email,
                AvatarUrl = newAccount.AvatarUrl,
                Role = primaryRole,
                IsNewAccount = true,
                AccountStatus = newAccount.Status.ToString(),
                VerificationStatus = verificationStatus
            };
            await _systemNotificationService.CreateAndSendNotificationAsync(newAccount.Id, "Welcome to Imate",null);

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                User = userDto
            };
        }
        public async Task<AuthResponse> VerifyFirebaseTokenAndLoginAsync(LoginRequest request)
        {
            if (string.IsNullOrEmpty(request?.FirebaseIdToken))
            {
                throw new UnauthorizedException("Firebase token không hợp lệ.");
            }

            FirebaseToken decodedToken;
            try
            {
                // BƯỚC 1: XÁC MINH FIREBASE ID TOKEN
                decodedToken = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseIdToken);
            }
            catch (FirebaseAuthException ex)
            {
                throw new UnauthorizedException($"Xác thực Firebase thất bại: {ex.Message}");
            }

            string uid = decodedToken.Uid;

            bool isEmailVerified = decodedToken.Claims.GetValueOrDefault("email_verified", false) as bool? ?? false;
            if (!isEmailVerified)
            {
                throw new UnauthorizedException("Vui lòng xác minh tài khoản email trước khi đăng nhập.");
            }

            // BƯỚC 3: TÌM TÀI KHOẢN TRONG DATABASE
            var account = await _accountRepository.GetByProviderIdAsync(uid);

            if (account == null)
            {
                throw new NotFoundException("Tài khoản chưa được đăng ký trong hệ thống.");
            }
            if (account.Status == AccountStatus.Suspended)
            {
                throw new AccountBannedException("Tài khoản của bạn đã bị vô hiệu hóa.");
            }

            // BƯỚC 4: TẠO VÀ TRẢ VỀ LOCAL JWT TOKEN
            var roles = await _roleService.GetRolesForAccountAsync(account.Id);
            var token = _jwtTokenGenerator.GenerateToken(account.Id, roles);
            
            // BƯỚC 5: TẠO VÀ LƯU REFRESH TOKEN
            var refreshToken = await CreateAndSaveRefreshTokenAsync(account.Id);
            
            var rolesList = roles.ToList();
            var primaryRole = rolesList.FirstOrDefault().ToString() ?? "Candidate";
            var verificationStatus = await GetVerificationStatusAsync(account.Id, primaryRole);

            var isNewAccount = (primaryRole == RoleName.Mentor.ToString() && (await _mentorRepository.GetMentorByIdAsync(account.Id)) == null)
                               || (primaryRole == RoleName.Recruiter.ToString() && (await _recruiterRepository.GetRecruiterByIdAsync(account.Id)) == null);

            var userDto = new UserDto
            {
                Id = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl,
                Role = primaryRole,
                IsNewAccount = isNewAccount,
                AccountStatus = account.Status.ToString(),
                VerificationStatus = verificationStatus
            };

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                User = userDto
            };
        }
        public async Task<AuthResponse> RegisterOrLoginWithGoogleAsync(RegisterWithGoogleRequest request)
        {
            FirebaseToken firebaseToken;
            try
            {
                firebaseToken = await _firebaseAuth.VerifyIdTokenAsync(request.IdToken);
            }
            catch (Exception)
            {
                throw new Exception("Token xác thực Google không hợp lệ hoặc đã hết hạn.");
            }

            var providerId = firebaseToken.Uid;
            var email = firebaseToken.Claims.GetValueOrDefault("email")?.ToString() ?? throw new Exception("Thiếu Email trong Token.");
            var fullName = firebaseToken.Claims.GetValueOrDefault("name")?.ToString() ?? "";
            var avatarUrl = firebaseToken.Claims.GetValueOrDefault("picture")?.ToString();

            var existingAccount = await _accountRepository.GetByProviderIdAsync(providerId);
            Account accountToUse;

            if (existingAccount == null)
            {
                if (await _accountRepository.ExistsByEmailAsync(email))
                {
                    throw new Exception("Email này đã được đăng ký bằng phương thức khác. Vui lòng đăng nhập bằng Email/Mật khẩu.");
                }

                var requestedRole = ConvertUserRoleToRoleName(request.Role);
                var accountStatus = (requestedRole == RoleName.Mentor || requestedRole == RoleName.Recruiter)
                    ? AccountStatus.PendingVerification
                    : AccountStatus.Active;

                accountToUse = new Account
                {
                    Email = email,
                    FullName = fullName,
                    AvatarUrl = avatarUrl,
                    Provider = LoginProvider.Google,
                    ProviderId = providerId,
                    Status = accountStatus,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _accountRepository.AddAsync(accountToUse);
                await _roleService.AssignDefaultRoleAsync(accountToUse.Id, requestedRole);
                await _systemNotificationService.CreateAndSendNotificationAsync(accountToUse.Id, "Đổi mật khẩu thành công", null);

            }
            else
            {
                accountToUse = existingAccount;
                if (accountToUse.Status == AccountStatus.Suspended)
                {
                    throw new AccountBannedException("Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.");
                }
            }

            var roles = await _roleService.GetRolesForAccountAsync(accountToUse.Id);
            var token = _jwtTokenGenerator.GenerateToken(accountToUse.Id, roles);
            
            // TẠO VÀ LƯU REFRESH TOKEN
            var refreshToken = await CreateAndSaveRefreshTokenAsync(accountToUse.Id);
            
            var rolesList = roles.ToList();
            var primaryRole = rolesList.FirstOrDefault().ToString() ?? "Candidate";
            
            // Khoảng existingAccount check
            var isNewAccount = (primaryRole == RoleName.Mentor.ToString() && (await _mentorRepository.GetMentorByIdAsync(accountToUse.Id)) == null)
                               || (primaryRole == RoleName.Recruiter.ToString() && (await _recruiterRepository.GetRecruiterByIdAsync(accountToUse.Id)) == null);
            var verificationStatus = await GetVerificationStatusAsync(accountToUse.Id, primaryRole);

            var userDto = new UserDto
            {
                Id = accountToUse.Id,
                FullName = accountToUse.FullName,
                Email = accountToUse.Email,
                AvatarUrl = accountToUse.AvatarUrl,
                Role = primaryRole,
                IsNewAccount = isNewAccount,
                AccountStatus = accountToUse.Status.ToString(),
                VerificationStatus = verificationStatus
            };

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                User = userDto
            };
        }

        private RoleName ConvertUserRoleToRoleName(string? userRole)
        {
            if (string.IsNullOrWhiteSpace(userRole))
            {
                return RoleName.Candidate; // Default
            }

            if (Enum.TryParse<RoleName>(userRole, true, out var parsedRole))
            {
                return parsedRole;
            }
            return RoleName.Candidate;
        }

        private async Task<string?> GetVerificationStatusAsync(int accountId, string primaryRole)
        {
            if (primaryRole == "Mentor")
            {
                var mentor = await _mentorRepository.GetMentorByIdAsync(accountId);
                return mentor?.VerificationStatus.ToString();
            }
            if (primaryRole == "Recruiter")
            {
                var recruiter = await _recruiterRepository.GetRecruiterByIdAsync(accountId);
                return recruiter?.VerificationStatus.ToString();
            }
            return null;
        }

        public async Task CreateEmployeeAccountAsync(int accountId, CreateEmployeeRequest request)
        {
            // 1. Kiểm tra Email đã tồn tại trong DB của bạn chưa
            if (await _accountRepository.ExistsByEmailAsync(request.Email))
            {
                throw new ConflictException("Email này đã tồn tại trong hệ thống.");
            }

            // 2. Tạo một mật khẩu ngẫu nhiên tạm thời (chỉ dùng 1 lần)
            // Nhân viên sẽ không bao giờ thấy mật khẩu này.
            string temporaryPassword = GenerateRandomPassword();

            UserRecord firebaseUser;
            try
            {
                // 3. TẠO USER TRÊN FIREBASE AUTH
                var args = new UserRecordArgs
                {
                    Email = request.Email,
                    Password = temporaryPassword,
                    DisplayName = request.FullName,
                    EmailVerified = true
                };
                firebaseUser = await _firebaseAuth.CreateUserAsync(args);
            }
            catch (Exception ex)
            {
                throw new Exception($"Tạo tài khoản Firebase thất bại: {ex.Message}");
            }

            // 4. TẠO ACCOUNT TRONG DATABASE CỦA BẠN
            var newAccount = new Account
            {
                Email = request.Email,
                FullName = request.FullName,
                Provider = LoginProvider.EmailPassword, 
                ProviderId = firebaseUser.Uid,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _accountRepository.AddAsync(newAccount);

            // 5. GÁN QUYỀN (ROLE) CHO NHÂN VIÊN
            // Giả sử bạn có RoleName.Employee
            await _roleService.AssignDefaultRoleAsync(newAccount.Id, RoleName.Staff);

            // 6. TẠO LINK RESET PASSWORD
            string resetLink;
            try
            {
                resetLink = await _firebaseAuth.GeneratePasswordResetLinkAsync(request.Email);
            }
            catch (Exception ex)
            {
                await _firebaseAuth.DeleteUserAsync(firebaseUser.Uid);
                await _accountRepository.DeleteAsync(newAccount);
                throw new Exception($"Tạo user thành công nhưng không thể tạo link reset: {ex.Message}");
            }
           
            var emailSubject = "Chào mừng bạn! Vui lòng thiết lập mật khẩu";
            var emailBody = GenerateEmployeeWelcomeTemplate(request.FullName, resetLink);
            await _emailService.SendEmailAsync(request.Email, emailSubject, emailBody);
            //await _emailService.SendEmailAsync("startingimate@gmail.com", emailSubject, emailBody);

            await _auditLogService.CreateAuditLogAsync(accountId, AuditAction.Create, "Account", newAccount.Id,
               new { },
               new
               {
                   newAccount.Email,
                   newAccount.FullName,
                   newAccount.CreatedAt,
                   
               }
            );
        }

        // Thêm hàm helper này vào cuối class AuthService
        private string GenerateRandomPassword(int length = 20)
        {
            const string validChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890!@#$%^&*()_+-=[]{}|;:,.<>?";
            var random = new Random();
            var password = new char[length];
            for (int i = 0; i < length; i++)
            {
                password[i] = validChars[random.Next(validChars.Length)];
            }
            return new string(password) + "aA1!"; 
        }

        public async Task ChangePasswordAsync(int accountId, ChangePasswordRequest request)
        {
            FirebaseToken decodedToken;
            try
            {
                // 1. Xác minh Firebase ID Token.
                // Client BẮT BUỘC phải re-authenticate TRƯỚC KHI gọi API này
                decodedToken = await _firebaseAuth.VerifyIdTokenAsync(request.FirebaseIdToken);
            }
            catch (FirebaseAuthException ex)
            {
                // Token không hợp lệ hoặc đã hết hạn
                throw new UnauthorizedException($"Xác thực Firebase thất bại: {ex.Message}. Vui lòng đăng nhập lại.");
            }

            // 2. Lấy UID (Firebase) và Email từ token
            string uid = decodedToken.Uid;
            var email = decodedToken.Claims.GetValueOrDefault("email")?.ToString();

            // 3. Lấy tài khoản local từ DB (dùng accountId từ JWT của API)
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new NotFoundException("Không tìm thấy tài khoản.");
            }

            // 4. (Quan trọng) Đối chiếu xem tài khoản local có khớp với token Firebase không
            if (account.ProviderId != uid || !string.Equals(account.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                // Đảm bảo user trong JWT (accountId) chính là user trong FirebaseIdToken (uid)
                throw new ForbiddenException("Hành động không được phép. Tài khoản không khớp.");
            }

            // 5. Chỉ cho phép đổi mật khẩu cho tài khoản Email/Password
            if (account.Provider != LoginProvider.EmailPassword)
            {
                throw new BadRequestException("Không thể đổi mật khẩu cho tài khoản đăng nhập bằng Google.");
            }

            if (!string.IsNullOrEmpty(request.CurrentPassword) && request.NewPassword == request.CurrentPassword)
            {
                throw new BadRequestException("Mật khẩu mới không được trùng với mật khẩu hiện tại.");
            }

            // 6. Cập nhật mật khẩu trên Firebase (Admin SDK)
            try
            {
                var args = new UserRecordArgs
                {
                    Uid = uid,
                    Password = request.NewPassword
                };
                await _firebaseAuth.UpdateUserAsync(args);
            }
            catch (Exception ex)
            {
                // Lỗi từ Firebase (ví dụ: mật khẩu quá yếu)
                throw new Exception($"Cập nhật mật khẩu trên Firebase thất bại: {ex.Message}");
            }

            // 7. Cập nhật local DB (ví dụ: ngày UpdateAt)
            account.UpdatedAt = DateTime.UtcNow;
            await _accountRepository.UpdateAsync(account);
            await _systemNotificationService.CreateAndSendNotificationAsync(accountId, "Bạn đã đổi mật khẩu thành công!", null);

            // 8. Revoke tất cả refresh tokens cũ để force logout tất cả devices
            // Điều này đảm bảo rằng sau khi đổi mật khẩu, user phải đăng nhập lại trên tất cả devices
            await _refreshTokenRepository.RevokeAllTokensForAccountAsync(accountId);
        }

        /// <summary>
        /// Tạo và lưu refresh token vào database
        /// </summary>
        private async Task<RefreshToken> CreateAndSaveRefreshTokenAsync(int accountId)
        {
            // Revoke tất cả refresh tokens cũ của account này (optional - có thể cho phép nhiều devices)
            // await _refreshTokenRepository.RevokeAllTokensForAccountAsync(accountId);

            var refreshTokenValue = _jwtTokenGenerator.GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenValue,
                AccountId = accountId,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpiryDays),
                CreatedAt = DateTime.UtcNow,
                IsRevoked = false
            };

            await _refreshTokenRepository.AddAsync(refreshToken);
            return refreshToken;
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshTokenValue)
        {
            // 1. Tìm refresh token trong database
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenValue);
            
            if (refreshToken == null)
            {
                throw new UnauthorizedException("Refresh token không hợp lệ.");
            }

            // 2. Kiểm tra refresh token đã bị revoke chưa
            if (refreshToken.IsRevoked)
            {
                throw new UnauthorizedException("Refresh token đã bị thu hồi.");
            }

            // 3. Kiểm tra refresh token đã hết hạn chưa
            if (refreshToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token đã hết hạn.");
            }

            // 4. Lấy account từ refresh token
            var account = refreshToken.Account;
            if (account == null)
            {
                throw new NotFoundException("Không tìm thấy tài khoản liên kết với refresh token.");
            }

            if (account.Status == AccountStatus.Suspended)
            {
                throw new AccountBannedException("Tài khoản của bạn đã bị vô hiệu hóa.");
            }

            // 5. Revoke refresh token cũ
            await _refreshTokenRepository.RevokeTokenAsync(refreshTokenValue);

            // 6. Tạo access token mới
            var roles = await _roleService.GetRolesForAccountAsync(account.Id);
            var newAccessToken = _jwtTokenGenerator.GenerateToken(account.Id, roles);

            // 7. Tạo refresh token mới
            var newRefreshToken = await CreateAndSaveRefreshTokenAsync(account.Id);

            var rolesList = roles.ToList();
            var primaryRole = rolesList.FirstOrDefault().ToString() ?? "Candidate";
            var verificationStatus = await GetVerificationStatusAsync(account.Id, primaryRole);

            var isNewAccount = (primaryRole == RoleName.Mentor.ToString() && (await _mentorRepository.GetMentorByIdAsync(account.Id)) == null)
                               || (primaryRole == RoleName.Recruiter.ToString() && (await _recruiterRepository.GetRecruiterByIdAsync(account.Id)) == null);

            // 8. Tạo UserDto
            var userDto = new UserDto
            {
                Id = account.Id,
                FullName = account.FullName,
                Email = account.Email,
                AvatarUrl = account.AvatarUrl,
                Role = primaryRole,
                IsNewAccount = isNewAccount,
                AccountStatus = account.Status.ToString(),
                VerificationStatus = verificationStatus
            };

            return new AuthResponse
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                User = userDto
            };
        }

        public async Task<string> GenerateActionCodeAsync(string email, string actionType)
        {
            try
            {
                // Kiểm tra provider cho PASSWORD_RESET - không cho phép reset password cho tài khoản Google
                if (actionType == "PASSWORD_RESET")
                {
                    var account = await _accountRepository.GetByEmailAsync(email);
                    if (account != null && account.Provider != LoginProvider.EmailPassword)
                    {
                        throw new BadRequestException("Không thể đặt lại mật khẩu cho tài khoản đăng nhập bằng Google. Vui lòng đăng nhập bằng Google để truy cập tài khoản.");
                    }
                }

                string actionLink;
                
                if (actionType == "VERIFY_EMAIL")
                {
                    // Generate email verification link
                    actionLink = await _firebaseAuth.GenerateEmailVerificationLinkAsync(email);
                }
                else if (actionType == "PASSWORD_RESET")
                {
                    // Generate password reset link
                    actionLink = await _firebaseAuth.GeneratePasswordResetLinkAsync(email);
                }
                else
                {
                    throw new ArgumentException($"Invalid action type: {actionType}");
                }

                // Extract oobCode from the link
                // Firebase link format: https://Imate-a820f.firebaseapp.com/__/auth/action?mode=...&oobCode=...&continueUrl=...
                var uri = new Uri(actionLink);
                var query = uri.Query.TrimStart('?');
                var queryParams = query.Split('&')
                    .Select(p => p.Split('='))
                    .Where(p => p.Length == 2)
                    .ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
                
                if (!queryParams.TryGetValue("oobCode", out var oobCode) || string.IsNullOrEmpty(oobCode))
                {
                    throw new InvalidOperationException("Failed to extract oobCode from Firebase action link");
                }

                return oobCode;
            }
            catch (BadRequestException)
            {
                // Re-throw BadRequestException without wrapping
                throw;
            }
            catch (FirebaseAuthException ex)
            {
                throw new NotFoundException($"Không tìm thấy tài khoản với email: {email}. {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể tạo action code: {ex.Message}");
            }
        }

        public async Task SendActionEmailAsync(string oobCode, string email, string actionType)
        {
            try
            {
                // Get backend base URL from configuration
                var backendBaseUrl = _configuration["BackendUrl"] ??
                                     _configuration["BackendUrl:BaseUrl"] ?? 
                                     _configuration["BackendSettings"] ?? 
                                     _configuration["BackendSettings:BaseUrl"] ??
                                    _configuration["ASPNETCORE_URLS"]?.Split(';').FirstOrDefault() ??
                                    "http://localhost:4078";
                
                // Build the action URL pointing to backend action-handler endpoint
                // This will redirect to frontend with the oobCode
                var actionUrl = $"{backendBaseUrl}/api/action-handler?mode={(actionType == "VERIFY_EMAIL" ? "verifyEmail" : "resetPassword")}&oobCode={Uri.EscapeDataString(oobCode)}";

                string emailSubject;
                string emailBody;

                if (actionType == "VERIFY_EMAIL")
                {
                    emailSubject = "Xác minh email của bạn - Imate";
                    emailBody = GenerateVerifyEmailTemplate(email, actionUrl);
                }
                else if (actionType == "PASSWORD_RESET")
                {
                    emailSubject = "Đặt lại mật khẩu - Imate";
                    emailBody = GenerateResetPasswordTemplate(email, actionUrl);
                }
                else
                {
                    throw new ArgumentException($"Invalid action type: {actionType}");
                }

                // Send email via Resend
                await _emailService.SendEmailAsync(email, emailSubject, emailBody);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể gửi email: {ex.Message}");
            }
        }

        private string GenerateVerifyEmailTemplate(string email, string actionUrl)
        {
            var logoUrl = $"{_frontendBaseUrl}/src/assets/images/logo.png";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Xác minh email - Imate</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <!-- Header with Logo -->
                    <tr>
                        <td align='center' style='padding: 40px 20px 20px;'>
                            <img src='{logoUrl}' alt='Imate Logo' style='max-width: 150px; height: auto;' />
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 20px 40px;'>
                            <h2 style='color: #333333; margin: 0 0 20px; font-size: 24px;'>Xác minh email của bạn</h2>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                                Xin chào,
                            </p>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                                Cảm ơn bạn đã đăng ký tài khoản Imate! Để hoàn tất quá trình đăng ký, vui lòng xác minh địa chỉ email của bạn bằng cách nhấp vào nút bên dưới:
                            </p>
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' style='padding: 30px 0;'>
                                        <a href='{actionUrl}' style='display: inline-block; padding: 14px 32px; background-color: #5d5fef; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: 600;'>Xác minh email</a>
                                    </td>
                                </tr>
                            </table>
                            <p style='color: #666666; font-size: 14px; line-height: 1.6; margin: 20px 0 0;'>
                                Nếu nút không hoạt động, bạn có thể sao chép và dán liên kết sau vào trình duyệt:
                            </p>
                            <p style='color: #5d5fef; font-size: 14px; word-break: break-all; margin: 10px 0 0;'>
                                {actionUrl}
                            </p>
                            <p style='color: #999999; font-size: 14px; line-height: 1.6; margin: 30px 0 0;'>
                                Liên kết này sẽ hết hạn sau 24 giờ.
                            </p>
                            <p style='color: #999999; font-size: 14px; line-height: 1.6; margin: 10px 0 0;'>
                                Nếu bạn không yêu cầu xác minh email này, vui lòng bỏ qua email này.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 30px 40px; background-color: #f9f9f9; border-top: 1px solid #eeeeee;'>
                            <p style='color: #999999; font-size: 12px; line-height: 1.6; margin: 0; text-align: center;'>
                                Trân trọng,<br/>
                                <strong style='color: #333333;'>Đội ngũ Imate</strong>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string GenerateResetPasswordTemplate(string email, string actionUrl)
        {
            var logoUrl = $"{_frontendBaseUrl}/src/assets/images/logo.png";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Đặt lại mật khẩu - Imate</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <!-- Header with Logo -->
                    <tr>
                        <td align='center' style='padding: 40px 20px 20px;'>
                            <img src='{logoUrl}' alt='Imate Logo' style='max-width: 150px; height: auto;' />
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 20px 40px;'>
                            <h2 style='color: #333333; margin: 0 0 20px; font-size: 24px;'>Đặt lại mật khẩu</h2>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                                Xin chào,
                            </p>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản Imate của bạn. Nhấp vào nút bên dưới để đặt lại mật khẩu:
                            </p>
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' style='padding: 30px 0;'>
                                        <a href='{actionUrl}' style='display: inline-block; padding: 14px 32px; background-color: #5d5fef; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: 600;'>Đặt lại mật khẩu</a>
                                    </td>
                                </tr>
                            </table>
                            <p style='color: #666666; font-size: 14px; line-height: 1.6; margin: 20px 0 0;'>
                                Nếu nút không hoạt động, bạn có thể sao chép và dán liên kết sau vào trình duyệt:
                            </p>
                            <p style='color: #5d5fef; font-size: 14px; word-break: break-all; margin: 10px 0 0;'>
                                {actionUrl}
                            </p>
                            <p style='color: #999999; font-size: 14px; line-height: 1.6; margin: 30px 0 0;'>
                                Liên kết này sẽ hết hạn sau 1 giờ.
                            </p>
                            <p style='color: #ff6b6b; font-size: 14px; line-height: 1.6; margin: 20px 0 0; padding: 15px; background-color: #fff5f5; border-left: 4px solid #ff6b6b; border-radius: 4px;'>
                                <strong>Lưu ý:</strong> Nếu bạn không yêu cầu đặt lại mật khẩu, vui lòng bỏ qua email này. Mật khẩu của bạn sẽ không thay đổi.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 30px 40px; background-color: #f9f9f9; border-top: 1px solid #eeeeee;'>
                            <p style='color: #999999; font-size: 12px; line-height: 1.6; margin: 0; text-align: center;'>
                                Trân trọng,<br/>
                                <strong style='color: #333333;'>Đội ngũ Imate</strong>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }

        private string GenerateEmployeeWelcomeTemplate(string fullName, string resetLink)
        {
            var logoUrl = $"{_frontendBaseUrl}/src/assets/images/logo.png";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Chào mừng bạn - Imate</title>
</head>
<body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f5f5f5;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background-color: #f5f5f5; padding: 40px 0;'>
        <tr>
            <td align='center'>
                <table width='600' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                    <!-- Header with Logo -->
                    <tr>
                        <td align='center' style='padding: 40px 20px 20px;'>
                            <img src='{logoUrl}' alt='Imate Logo' style='max-width: 150px; height: auto;' />
                        </td>
                    </tr>
                    <!-- Content -->
                    <tr>
                        <td style='padding: 20px 40px;'>
                            <h2 style='color: #333333; margin: 0 0 20px; font-size: 24px;'>Chào mừng bạn đến với Imate!</h2>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                                Xin chào {fullName},
                            </p>
                            <p style='color: #666666; font-size: 16px; line-height: 1.6; margin: 0 0 20px;'>
                                Một tài khoản đã được tạo cho bạn trên hệ thống Imate. Để bắt đầu sử dụng tài khoản, vui lòng thiết lập mật khẩu của bạn bằng cách nhấp vào nút bên dưới:
                            </p>
                            <table width='100%' cellpadding='0' cellspacing='0'>
                                <tr>
                                    <td align='center' style='padding: 30px 0;'>
                                        <a href='{resetLink}' style='display: inline-block; padding: 14px 32px; background-color: #5d5fef; color: #ffffff; text-decoration: none; border-radius: 6px; font-size: 16px; font-weight: 600;'>Thiết lập mật khẩu</a>
                                    </td>
                                </tr>
                            </table>
                            <p style='color: #666666; font-size: 14px; line-height: 1.6; margin: 20px 0 0;'>
                                Nếu nút không hoạt động, bạn có thể sao chép và dán liên kết sau vào trình duyệt:
                            </p>
                            <p style='color: #5d5fef; font-size: 14px; word-break: break-all; margin: 10px 0 0;'>
                                {resetLink}
                            </p>
                            <p style='color: #999999; font-size: 14px; line-height: 1.6; margin: 30px 0 0;'>
                                Liên kết này sẽ hết hạn sau 1 giờ.
                            </p>
                            <p style='color: #4caf50; font-size: 14px; line-height: 1.6; margin: 20px 0 0; padding: 15px; background-color: #f1f8f4; border-left: 4px solid #4caf50; border-radius: 4px;'>
                                <strong>Lưu ý:</strong> Sau khi thiết lập mật khẩu, bạn có thể đăng nhập vào hệ thống Imate và bắt đầu sử dụng các tính năng dành cho nhân viên.
                            </p>
                        </td>
                    </tr>
                    <!-- Footer -->
                    <tr>
                        <td style='padding: 30px 40px; background-color: #f9f9f9; border-top: 1px solid #eeeeee;'>
                            <p style='color: #999999; font-size: 12px; line-height: 1.6; margin: 0; text-align: center;'>
                                Trân trọng,<br/>
                                <strong style='color: #333333;'>Đội ngũ Imate</strong>
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
        }
    }
}
