using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Presentation.RequestModels.Speech;

namespace Imate.API.Presentation.Controllers
{
    /// <summary>
    /// Controller for Speech Recognition (Speech-to-Text)
    /// Supports Vietnamese and English audio transcription
    /// </summary>
    [Route("api/azure-speech")]
    [ApiController]
    public class SpeechRecognitionController : ControllerBase
    {
        private readonly IAzureSpeechRecognitionService _speechRecognitionService;
        private readonly ILogger<SpeechRecognitionController> _logger;

        public SpeechRecognitionController(
            IAzureSpeechRecognitionService speechRecognitionService,
            ILogger<SpeechRecognitionController> logger)
        {
            _speechRecognitionService = speechRecognitionService;
            _logger = logger;
        }

        /// <summary>
        /// Convert audio file to text (Speech-to-Text)
        /// </summary>
        /// <param name="request">Request containing audio file and language</param>
        /// <returns>Transcribed text</returns>
        [HttpPost("recognize")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> RecognizeSpeech([FromForm] RecognizeSpeechFromFileRequest request)
        {
            try
            {
                // Validate input
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (request.AudioFile == null || request.AudioFile.Length == 0)
                {
                    return BadRequest(new { message = "Audio file is required" });
                }

                var audioFile = request.AudioFile;
                var language = request.Language ?? "vi-VN";

                // Validate file size (max 10MB)
                const long maxFileSize = 10 * 1024 * 1024; // 10MB
                if (audioFile.Length > maxFileSize)
                {
                    return BadRequest(new { message = $"Audio file size exceeds maximum allowed size of {maxFileSize / (1024 * 1024)}MB" });
                }

                // Validate language code
                var supportedLanguages = new[] { "vi-VN", "en-US", "en-GB" };
                if (!supportedLanguages.Contains(language))
                {
                    return BadRequest(new { message = $"Unsupported language. Supported languages: {string.Join(", ", supportedLanguages)}" });
                }

                _logger.LogInformation($"Processing audio file: {audioFile.FileName}, Size: {audioFile.Length} bytes, Language: {language}");

                // Read audio file into memory
                using var audioStream = audioFile.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await audioStream.CopyToAsync(memoryStream);
                var audioBytes = memoryStream.ToArray();

                // Perform speech recognition
                var transcribedText = await _speechRecognitionService.RecognizeSpeechAsync(audioBytes, language);

                _logger.LogInformation($"Speech recognition completed. Transcribed text length: {transcribedText?.Length ?? 0}");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        text = transcribedText,
                        language = language,
                        audioFileName = audioFile.FileName,
                        audioFileSize = audioFile.Length
                    },
                    message = "Speech recognition completed successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Speech recognition failed");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during speech recognition");
                return StatusCode(500, new { success = false, message = "An error occurred during speech recognition. Please try again." });
            }
        }

        /// <summary>
        /// Convert audio data (base64 encoded) to text
        /// </summary>
        /// <param name="request">Request containing base64 audio data and language</param>
        /// <returns>Transcribed text</returns>
        [HttpPost("recognize-base64")]
        public async Task<IActionResult> RecognizeSpeechFromBase64([FromBody] RecognizeSpeechRequest request)
        {
            try
            {
                // Validate input
                if (string.IsNullOrWhiteSpace(request.AudioData))
                {
                    return BadRequest(new { message = "Audio data is required" });
                }

                // Validate language code
                var supportedLanguages = new[] { "vi-VN", "en-US", "en-GB" };
                var language = request.Language ?? "vi-VN";
                if (!supportedLanguages.Contains(language))
                {
                    return BadRequest(new { message = $"Unsupported language. Supported languages: {string.Join(", ", supportedLanguages)}" });
                }

                _logger.LogInformation($"Processing base64 audio data, Language: {language}");

                // Decode base64 audio data
                byte[] audioBytes;
                try
                {
                    // Remove data URL prefix if present (e.g., "data:audio/wav;base64,...")
                    var base64Data = request.AudioData;
                    if (base64Data.Contains(","))
                    {
                        base64Data = base64Data.Split(',')[1];
                    }
                    audioBytes = Convert.FromBase64String(base64Data);
                }
                catch (FormatException)
                {
                    return BadRequest(new { message = "Invalid base64 audio data format" });
                }

                // Perform speech recognition
                var transcribedText = await _speechRecognitionService.RecognizeSpeechAsync(audioBytes, language ?? "vi-VN");

                _logger.LogInformation($"Speech recognition completed. Transcribed text length: {transcribedText?.Length ?? 0}");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        text = transcribedText,
                        language = language,
                        audioDataSize = audioBytes.Length
                    },
                    message = "Speech recognition completed successfully"
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Speech recognition failed");
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during speech recognition");
                return StatusCode(500, new { success = false, message = "An error occurred during speech recognition. Please try again." });
            }
        }

        /// <summary>
        /// Request model for base64 audio recognition
        /// </summary>
        public class RecognizeSpeechRequest
        {
            /// <summary>
            /// Base64 encoded audio data
            /// </summary>
            public string AudioData { get; set; } = string.Empty;

            /// <summary>
            /// Language code (vi-VN for Vietnamese, en-US for English). Default: vi-VN
            /// </summary>
            public string? Language { get; set; } = "vi-VN";
        }
    }
}

