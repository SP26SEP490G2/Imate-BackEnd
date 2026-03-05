using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces;
using Imate.API.Business.Interfaces.UserManagement;
using Imate.API.Business.Exceptions;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels.UserManagement;
using Imate.API.Presentation.ResponseModels.UserManagement;

namespace Imate.API.Presentation.Controllers.UserManagement
{
    [Route("api")]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private readonly Business.Interfaces.UserManagement.IAccountService _accountService;
        private readonly IAuditLogService _auditLogService;
        private readonly IUnitOfWork _unitOfWork;

        public AccountsController(Business.Interfaces.UserManagement.IAccountService accountService, IAuditLogService auditLogService, IUnitOfWork unitOfWork)
        {
            _accountService = accountService;
            _auditLogService = auditLogService;
            _unitOfWork = unitOfWork;
        }

        private int? GetCurrentUserId()
        {
            if (!User.Identity.IsAuthenticated) return null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : null;
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> GetAllAccountAsync([FromQuery] AccountParams accountParams)
        {
            var pagedResult = await _accountService.GetAllAccountAsync(accountParams);
            Response.Headers.Add("X-Pagination",
               System.Text.Json.JsonSerializer.Serialize(
            new
            {
                pagedResult.TotalCount,
                pagedResult.PageSize,
                pagedResult.PageNumber,
                pagedResult.TotalPages
            }));

            return Ok(pagedResult);

        }
        [HttpGet("accounts/{id}")]
        public async Task<IActionResult> GetAccountByIdAsync(int id)
        {
            var account = await _accountService.GetAccountByIdAsync(id);

            return Ok(account);
        }
        [HttpPut("accounts/{id}/{status}")]
        public async Task<IActionResult> UpdateAccountStatusAsync(int id, string status)
        {
            // Get old value before update
            var existingAccount = await _unitOfWork.Accounts.GetByIdAsync(id);
            if (existingAccount == null)
            {
                return NotFound(new { Message = $"Không tìm thấy tài khoản với ID {id}" });
            }

            var oldValue = new { Status = existingAccount.Status.ToString() };

            var updatedAccount = await _accountService.UpdateAccountStatusAsync(id, status);

            // Get new value after update
            var newValue = new { Status = updatedAccount.Status.ToString() };

            // Create audit log - only for SuspendUser action
            var userId = GetCurrentUserId();
            if (userId.HasValue && Enum.TryParse(status, true, out AccountStatus newStatus) && newStatus == AccountStatus.Suspended)
            {
                await _auditLogService.CreateAuditLogAsync(
                    userId.Value,
                    AuditAction.SuspendUser,
                    "Account",
                    id,
                    oldValue,
                    newValue
                );
            }

            return Ok(new
            {
                Message = "Cập nhật trạng thái tài khoản thành công",
                updatedAccount
            });
        }

        [HttpGet("profile")]
        [Authorize]
        [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                // Lấy ID người dùng từ claims trong JWT token
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdString, out var accountId))
                {
                    return Unauthorized("Token không hợp lệ hoặc không chứa ID người dùng.");
                }
                var subscription = User.FindFirstValue("SubscriptionPackage");
                var userProfile = await _accountService.GetUserProfileAsync(accountId, subscription);

                return Ok(userProfile);
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

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateGeneralProfileRequest request)
        {
            try
            {
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdString, out var accountId))
                {
                    return Unauthorized("Token không hợp lệ hoặc không chứa ID người dùng.");
                }
                await _accountService.UpdateGeneralProfileAsync(accountId, request);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
        }

        [HttpPost("profile/mentor")]
        [Authorize]
        public async Task<IActionResult> SubmitMentorProfile([FromBody] UpdateMentorProfileRequest request)
        {
            try
            {
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdString, out var accountId))
                {
                    return Unauthorized("Token không hợp lệ hoặc không chứa ID người dùng.");
                }

                await _accountService.SubmitMentorProfileAsync(accountId, request);
                return NoContent();
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
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }

        [HttpPut("profile/role")]
        [Authorize]
        public async Task<IActionResult> UpdateMyRole([FromBody] UpdateUserRoleRequest request)
        {
            try
            {
                var accountIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(accountIdString, out var accountId))
                {
                    return Unauthorized("Token không hợp lệ hoặc không chứa ID người dùng.");
                }

                await _accountService.UpdateUserRoleAsync(accountId, request.Role);
                return Ok(new { message = "Cập nhật role thành công." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Status = 404, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Status = 400, Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Status = 500, Message = ex.Message });
            }
        }
        [HttpGet("accounts/staff/{accountId}")]
        public async Task<IActionResult> GetAccountDetailStaff(int accountId)
        {
            var accountDetail = await _accountService.GetAccountDetailStaff(accountId);
            return Ok(accountDetail);
        }
        [HttpGet("accounts/mentor/{accountId}")]
        public async Task<IActionResult> GetAccountDetailMentor(int accountId)
        {
            var accountDetail = await _accountService.GetAccountDetailMentor(accountId);
            return Ok(accountDetail);
        }
        [HttpGet("accounts/candidate/{accountId}")]
        public async Task<IActionResult> GetAccountDetailCandidate(int accountId)
        {
            var accountDetail = await _accountService.GetAccountDetailCandidate(accountId);
            return Ok(accountDetail);
        }
        [HttpGet("accounts/overview")]
        public async Task<IActionResult> GetAccountOverviewAsync()
        {
            var overview = await _accountService.GetAccountOverview();
            return Ok(overview);
        }

    }
}
