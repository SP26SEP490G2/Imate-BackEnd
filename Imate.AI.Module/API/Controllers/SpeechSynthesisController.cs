using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.API.Controllers
{
    /// <summary>
    /// Controller tổng hợp giọng nói — Azure Speech TTS
    /// Route: /api/speech/*
    /// </summary>
    [ApiController]
    [Route("api/speech")]
    [Authorize]
    public class SpeechSynthesisController : ControllerBase
    {
        private readonly ISpeechSynthesisService _speechSynthesisService;
        private readonly ILogger<SpeechSynthesisController> _logger;

        public SpeechSynthesisController(
            ISpeechSynthesisService speechSynthesisService,
            ILogger<SpeechSynthesisController> logger)
        {
            _speechSynthesisService = speechSynthesisService;
            _logger = logger;
        }

        /// <summary>
        /// Chuyển text thành giọng nói (TTS)
        /// POST /api/speech/synthesize
        /// </summary>
        [HttpPost("synthesize")]
        public async Task<IActionResult> Synthesize([FromBody] SynthesizeSpeechRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Yêu cầu không hợp lệ", errors = ModelState });

            try
            {
                _logger.LogInformation("TTS request. Language: {Language}, Voice: {Voice}, Length: {Length}",
                    request.Language, request.Voice, request.Text.Length);

                var result = await _speechSynthesisService.SynthesizeToBase64Async(
                    request.Text,
                    request.Language,
                    request.Voice,
                    request.SpeechRate,
                    cancellationToken);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        text = result.Text,
                        audioUrl = result.AudioUrl,
                        audioBase64 = result.AudioBase64,
                        mimeType = result.MimeType,
                        voice = result.Voice,
                        language = result.Language
                    },
                    message = "Tổng hợp giọng nói thành công."
                });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid TTS request");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "TTS failed");
                return StatusCode(500, new { success = false, message = $"Không thể tổng hợp giọng nói: {ex.Message}" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected TTS error");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tổng hợp giọng nói." });
            }
        }
    }
}