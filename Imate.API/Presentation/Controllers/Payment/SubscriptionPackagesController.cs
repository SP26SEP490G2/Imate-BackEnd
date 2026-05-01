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

        [HttpPut(APIConfig.Subscription.UpdateSubscriptionPackageBenefits)]
        public async Task<IActionResult> UpdateSubscriptionPackageBenefitsAsync(int id, [FromBody] UpdatePackageBenefitsRequest request)
        {
            try
            {
                await _subscriptionPackageService.UpdatePackageBenefitsAsync(id, request.Benefits);
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

        [HttpPut(APIConfig.Subscription.UpdateSubscriptionPackageName)]
        public async Task<IActionResult> UpdateSubscriptionPackageNameAsync(int id, [FromBody] UpdatePackageNameRequest request)
        {
            try
            {
                await _subscriptionPackageService.UpdatePackageNameAsync(id, request.Name);
                return Ok(new { Message = Messages.MSG09 });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
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

        [HttpPost(APIConfig.Subscription.CreateSubscriptionPackage)]
        public async Task<IActionResult> CreateSubscriptionPackageAsync([FromBody] CreatePackageRequest request)
        {
            try
            {
                var response = await _subscriptionPackageService.CreatePackageAsync(
                    request.Name,
                    request.Price,
                    request.DurationDays,
                    request.Benefits,
                    request.IsRecommended
                );
                return Created("", new { Message = "Tạo gói dịch vụ thành công", data = response });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { Message = Messages.MSG10 });
            }
        }

        [HttpDelete(APIConfig.Subscription.DeleteSubscriptionPackage)]
        public async Task<IActionResult> DeleteSubscriptionPackageAsync(int id)
        {
            try
            {
                await _subscriptionPackageService.DeactivatePackageAsync(id);
                return Ok(new { Message = "Đã ẩn gói dịch vụ thành công." });
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
