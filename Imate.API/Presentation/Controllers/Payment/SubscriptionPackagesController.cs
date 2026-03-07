using Imate.API.Business.Interfaces.Payment;
using Imate.API.Common.Router;
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
    }
}
