using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS.Models.Webhooks;
using Imate.API.Business.Interfaces.Payment;

namespace Imate.API.Presentation.Controllers.Payment
{
    [ApiController]
    [Route("api")]
    public class WebhookController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        public WebhookController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost("webhook/payos")]
        [AllowAnonymous]
        public async Task<IActionResult> PayOsWebhook([FromBody] Webhook webhook)
        {
            try
            {
                await _transactionService.HandlePayOsWebhookAsync(webhook);
                return Ok();
            }
            catch (Exception ex)
            {
                // Ghi log
                return StatusCode(500, ex.Message);
            }
        }
    }
}
