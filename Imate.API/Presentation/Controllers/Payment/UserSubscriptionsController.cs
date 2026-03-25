using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces.Payment;

namespace Imate.API.Presentation.Controllers.Payment
{
    [Route("api")]
    [ApiController]
    public class UserSubscriptionsController : ControllerBase
    {
        private readonly IUserSubscriptionService _userSubscriptionService;
        public UserSubscriptionsController(IUserSubscriptionService userSubscriptionService)
        {
            _userSubscriptionService = userSubscriptionService;
        }

        [HttpPost("user-subscriptions")]
        public async Task<IActionResult> ActivateNewSubscriptionAsync([FromQuery] int accountId, [FromQuery] int packageId)
        {
            await _userSubscriptionService.ActivateNewSubscriptionAsync(accountId, packageId);
            return Ok();
        }
        [HttpGet("user-subscriptions/upgrade-preview")]
        public async Task<IActionResult> GetUpgradePreviewAsync([FromQuery] int accountId, [FromQuery] int newPackageId)
        {
            var preview = await _userSubscriptionService.GetUpgradePreviewAsync(accountId, newPackageId);
            return Ok(preview);
        }
        [HttpPost("user-subscriptions/cancel")]
        public async Task<IActionResult> CancelSubscriptionAsync([FromQuery] int accountId)
        {
            await _userSubscriptionService.CancelSubscriptionAsync(accountId);
            return Ok();
        }
        [HttpGet("user-subscriptions/cancel-preview")]
        public async Task<IActionResult> GetCancelPreview([FromQuery] int accountId)
        {
           var preview = await _userSubscriptionService.GetCancelPreviewAsync(accountId);
            return Ok(preview);
        }

        [HttpGet("user-subscriptions/history")]
        public async Task<IActionResult> GetUserSubscriptionHistory([FromQuery] int accountId)
        {
            var history = await _userSubscriptionService.GetUserSubscriptionHistoryAsync(accountId);
            return Ok(history);
        }

    }
}
