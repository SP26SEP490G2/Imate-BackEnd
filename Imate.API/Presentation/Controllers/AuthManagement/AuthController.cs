using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Presentation.ResponseModels.UserManagement;
using Imate.API.Business.Exceptions;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.Business.Interfaces;
using Imate.API.Common.Router;

namespace Imate.API.Presentation.Controllers.AuthManagement
{
    [Route("api")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IRoleService _roleService;
        private readonly IConfiguration _configuration;

        public AuthController(
            IAuthService authService,
            IJwtTokenGenerator jwtTokenGenerator,
            IRoleService roleService,
            IConfiguration configuration)
        {
            _authService = authService;
            _jwtTokenGenerator = jwtTokenGenerator;
            _roleService = roleService;
            _configuration = configuration;
        }


        [HttpPost(APIConfig.Authentication.RegisterEmail)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterWithEmail([FromBody] RegisterWithEmailRequest request)
        {
            try
            {
                var authResponse = await _authService.RegisterWithEmailAsync(request);

                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                if (ex is ConflictException)
                    return Conflict(new { Status = 409, Message = ex.Message });

                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpPost(APIConfig.Authentication.RegisterLoginWithGoogle)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RegisterOrLoginWithGoogle([FromBody] RegisterWithGoogleRequest request)
        {
            try
            {
                // THAY ĐỔI 2: _authService giờ trả về đối tượng AuthResponse hoàn chỉnh
                var authResponse = await _authService.RegisterOrLoginWithGoogleAsync(request);

                // Trả về thẳng đối tượng nhận được từ service
                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpPost(APIConfig.Authentication.LoginEmail)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> LoginWithEmail([FromBody] RequestModels.UserManagement.LoginRequest request)
        {
            try
            {
                // THAY ĐỔI 2: _authService giờ trả về đối tượng AuthResponse hoàn chỉnh
                var authResponse = await _authService.VerifyFirebaseTokenAndLoginAsync(request);
                // Trả về thẳng đối tượng nhận được từ service
                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedException)
                    return Unauthorized(new { Status = 401, Message = ex.Message });

                if (ex is NotFoundException)
                    return NotFound(new { Status = 404, Message = ex.Message });

                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpPost(APIConfig.Authentication.RefreshAuthenToken)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                var authResponse = await _authService.RefreshTokenAsync(request.RefreshToken);
                return Ok(authResponse);
            }
            catch (Exception ex)
            {
                if (ex is UnauthorizedException)
                    return Unauthorized(new { Status = 401, Message = ex.Message });

                if (ex is NotFoundException)
                    return NotFound(new { Status = 404, Message = ex.Message });

                if (ex is AccountBannedException)
                    return Forbid();

                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpPost("create-employee")]
        public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeRequest request)
        {
            try
            {
                var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("accountId")?.Value;

                if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
                    return Unauthorized(new { message = "Không thể xác định thông tin người dùng." });

                await _authService.CreateEmployeeAccountAsync(accountId, request);
                return Ok(new { Message = "Tạo tài khoản nhân viên thành công" });
            }
            catch (ConflictException ex)
            {
                // E2: Duplicate Email
                return Conflict(new { Status = 409, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }

        [HttpPut(APIConfig.Authentication.ChangePassword)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdString, out var accountId))
                {
                    return Unauthorized("Token không hợp lệ.");
                }

                await _authService.ChangePasswordAsync(accountId, request);
                return Ok(new { Message = "Đổi mật khẩu thành công." });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                return Unauthorized(new { Status = 401, Message = ex.Message });
            }
            catch (ForbiddenException ex)
            {
                return Forbid();
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Status = 404, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpPost(APIConfig.Authentication.GenerateActionCode)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> GenerateActionCode([FromBody] GenerateActionCodeRequest request)
        {
            try
            {
                var oobCode = await _authService.GenerateActionCodeAsync(request.Email, request.ActionType);
                return Ok(new { OobCode = oobCode });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Status = 404, Message = ex.Message });
            }
            catch (BadRequestException ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpPost(APIConfig.Authentication.SendActionEmail)]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendActionEmail([FromBody] SendActionEmailRequest request)
        {
            try
            {
                await _authService.SendActionEmailAsync(request.OobCode, request.Email, request.ActionType);
                return Ok(new { Message = "Email đã được gửi thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpGet(APIConfig.Authentication.ActionAuthenHandler)]
        [AllowAnonymous]
        public IActionResult ActionHandler([FromQuery] string mode, [FromQuery] string oobCode)
        {
            // This endpoint receives the oobCode and redirects to the appropriate frontend page
            if (string.IsNullOrEmpty(oobCode))
            {
                return BadRequest(new { Message = "Invalid action code" });
            }

            // Get frontend base URL from configuration
            var frontendBaseUrl = _configuration["FrontendSettings:BaseUrl"] ??
                                 throw new InvalidOperationException("FrontendSettings:BaseUrl is not set in appsettings");

            string redirectUrl;
            if (mode == "verifyEmail")
            {
                redirectUrl = $"{frontendBaseUrl}/verify-email?oobCode={Uri.EscapeDataString(oobCode)}";
            }
            else if (mode == "resetPassword")
            {
                redirectUrl = $"{frontendBaseUrl}/reset-password?oobCode={Uri.EscapeDataString(oobCode)}";
            }
            else
            {
                return BadRequest(new { Message = "Invalid action mode" });
            }

            // Return HTML that redirects to frontend
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta http-equiv='refresh' content='0; url={redirectUrl}'>
    <script>window.location.href = '{redirectUrl}';</script>
</head>
<body>
    <p>Đang chuyển hướng... <a href='{redirectUrl}'>Nhấp vào đây nếu không tự động chuyển hướng</a></p>
</body>
</html>";

            return Content(html, "text/html");
        }

#if DEBUG 
        [HttpGet("generate-test-token/{accountId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GenerateTestToken(int accountId)
        {
            try
            {
                var roles = await _roleService.GetRolesForAccountAsync(accountId);
                if (roles == null || !roles.Any())
                {
                    return NotFound(new { Message = $"Không tìm thấy tài khoản hoặc vai trò cho Account ID: {accountId}" });
                }

                var token = _jwtTokenGenerator.GenerateToken(accountId, roles);

                return Ok(new { Token = token });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Lỗi khi tạo token: {ex.Message}" });
            }
        }
#endif
    }
}
