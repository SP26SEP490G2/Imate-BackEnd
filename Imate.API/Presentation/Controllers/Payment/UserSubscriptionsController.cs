using Imate.API.Business.Interfaces.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Imate.API.Presentation.Controllers.Payment
{
    [Route("api/user-subscriptions")]
    [ApiController]
    [Authorize]
    public class UserSubscriptionsController : ControllerBase
    {
        private readonly IUserSubscriptionService _userSubscriptionService;

        public UserSubscriptionsController(IUserSubscriptionService userSubscriptionService)
        {
            _userSubscriptionService = userSubscriptionService;
        }

        [HttpPost("{packageId}")]
        public async Task<IActionResult> ActivateNewSubscriptionAsync(int packageId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                await _userSubscriptionService.ActivateNewSubscriptionAsync(accountId, packageId);

                return Ok(new
                {
                    message = "Đăng ký gói thành công."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("upgrade-preview/{newPackageId}")]
        public async Task<IActionResult> GetUpgradePreviewAsync(int newPackageId)
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var preview = await _userSubscriptionService.GetUpgradePreviewAsync(accountId, newPackageId);
                return Ok(preview);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> CancelSubscriptionAsync()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                await _userSubscriptionService.CancelSubscriptionAsync(accountId);

                return Ok(new
                {
                    message = "Hủy gói thành công."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpGet("cancel-preview")]
        public async Task<IActionResult> GetCancelPreview()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var preview = await _userSubscriptionService.GetCancelPreviewAsync(accountId);
                return Ok(preview);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpGet("history")]
        public async Task<IActionResult> GetUserSubscriptionHistory()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var history = await _userSubscriptionService.GetUserSubscriptionHistoryAsync(accountId);
                return Ok(history);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        [HttpGet("current-package")]
        public async Task<IActionResult> GetCurrentPackage()
        {
            try
            {
                var accountId = GetCurrentAccountId();
                var package = await _userSubscriptionService.GetCurrentPackageAsync(accountId);
                return Ok(package);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        private int GetCurrentAccountId()
        {
            var accountIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("accountId")?.Value;

            if (accountIdClaim == null || !int.TryParse(accountIdClaim, out int accountId))
            {
                throw new UnauthorizedAccessException("Invalid token: AccountId not found");
            }

            return accountId;
        }
    }
}
