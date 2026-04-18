using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Responses;
using Imate.AI.Module.Models.Requests;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.API.Controllers
{
    [Route("api/azure-speech")]
    [ApiController]
    //[Authorize]
    public class AzureSpeechSynthesisController : ControllerBase
    {
        private readonly IAzureSpeechSynthesisService _speechSynthesisService;
        private readonly ILogger<AzureSpeechSynthesisController> _logger;

        public AzureSpeechSynthesisController(
            IAzureSpeechSynthesisService speechSynthesisService,
            ILogger<AzureSpeechSynthesisController> logger)
        {
            _speechSynthesisService = speechSynthesisService;
            _logger = logger;
        }

        [HttpPost("synthesize")]
        public async Task<IActionResult> Synthesize([FromBody] AzureSynthesizeSpeechRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Yêu cầu không hợp lệ", errors = ModelState });
            }

            try
            {
                _logger.LogInformation("Synthesizing text with Azure Speech. Language: {Language}, Voice: {Voice}, Length: {Length}, ReturnBase64: {ReturnBase64}",
                    request.Language, request.Voice, request.Text.Length, request.ReturnBase64);

                AzureSynthesizedSpeechResult result;
                if (request.ReturnBase64)
                {
                    // Use base64 method (faster, no S3 upload)
                    result = await _speechSynthesisService.SynthesizeToBase64Async(
                        request.Text,
                        request.Language,
                        request.Voice,
                        request.SpeechRate,
                        cancellationToken);
                }
                else
                {
                    // Use S3 upload method
                    result = await _speechSynthesisService.SynthesizeAsync(
                        request.Text,
                        request.Language,
                        request.Voice,
                        request.SpeechRate,
                        cancellationToken);
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        text = result.Text,
                        audioUrl = result.AudioUrl,
                        audioBase64 = result.AudioBase64,
                        voice = result.Voice,
                        language = result.Language
                    },
                    message = "Synthesized speech successfully"
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid text for synthesis");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Speech synthesis failed");
                return StatusCode(500, new { success = false, message = $"Không thể tổng hợp giọng nói: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during speech synthesis");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tổng hợp giọng nói" });
            }
        }
    }
}

