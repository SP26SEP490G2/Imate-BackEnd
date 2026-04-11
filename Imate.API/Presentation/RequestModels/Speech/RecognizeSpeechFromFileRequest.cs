using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Imate.API.Presentation.RequestModels.Speech
{
    /// <summary>
    /// Request model for speech recognition from audio file
    /// </summary>
    public class RecognizeSpeechFromFileRequest
    {
        /// <summary>
        /// Audio file (WAV format recommended)
        /// </summary>
        [Required(ErrorMessage = "Audio file is required")]
        public IFormFile AudioFile { get; set; } = null!;

        /// <summary>
        /// Language code (vi-VN for Vietnamese, en-US for English). Default: vi-VN
        /// </summary>
        public string? Language { get; set; } = "vi-VN";
    }
}

