using Imate.API.Business.Exceptions;
using Imate.API.Business.Interfaces.Payment;
using Imate.API.Common;
using Imate.API.Common.Router;
using Imate.API.Presentation.RequestModels.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Imate.API.Presentation.Controllers.Payment
{
    [Route("api")]
    [ApiController]
    public class SubscriptionPackagesController : ControllerBase
    {
        private readonly ISubscriptionPackageService _subscriptionPackageService;

        public SubscriptionPackagesController(ISubscriptionPackageService subscriptionPackageService)
        {
            _subscriptionPackageService = subscriptionPackageService;
        }

        [AllowAnonymous]
        [HttpGet(APIConfig.Subscription.GetSubscriptionPackages)]
        public async Task<IActionResult> GetSubscriptionPackagesAsync()
        {
            var packages = await _subscriptionPackageService.GetPublicSubscriptionPackagesAsync();

            return Ok(new
            {
                data = packages
            });
        }

        [HttpGet(APIConfig.Subscription.GetSubscriptionOverview)]
        public async Task<IActionResult> GetSubscriptionOverviewAsync()
        {
            try
            {
                var overview = await _subscriptionPackageService.GetSubscriptionOverviewAsync();
                return Ok(overview);
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = Messages.MSG07 });
            }
        }

        [HttpPut(APIConfig.Subscription.UpdateSubscriptionPackagePrice)]
        public async Task<IActionResult> UpdateSubscriptionPackagePriceAsync(int id, [FromBody] UpdatePackagePriceRequest request)
        {
            try
            {
                await _subscriptionPackageService.UpdatePackagePriceAsync(id, request.Price);
                return Ok(new { Message = Messages.MSG09 });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = Messages.MSG10 });
            }
        }
    }
}
