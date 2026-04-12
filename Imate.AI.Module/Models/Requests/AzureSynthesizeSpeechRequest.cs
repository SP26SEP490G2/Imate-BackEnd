using System.ComponentModel.DataAnnotations;

namespace Imate.AI.Module.Models.Requests
{
    public class AzureSynthesizeSpeechRequest
    {
        [Required]
        [StringLength(5000, MinimumLength = 1)]
        public string Text { get; set; } = string.Empty;

        [RegularExpression(@"^[a-z]{2}-[A-Z]{2}$", ErrorMessage = "Language must be in BCP-47 format (e.g., vi-VN, en-US).")]
        public string? Language { get; set; }

        [StringLength(100)]
        public string? Voice { get; set; }

        /// <summary>
        /// If true, returns base64 audio instead of uploading to S3 (faster)
        /// </summary>
        public bool ReturnBase64 { get; set; } = false;

        /// <summary>
        /// Speech rate multiplier (0.5 = half speed, 1.0 = normal, 1.5 = 1.5x speed, 2.0 = double speed)
        /// Valid range: 0.5 to 2.0
        /// </summary>
        [Range(0.5, 2.0, ErrorMessage = "Speech rate must be between 0.5 and 2.0")]
        public double? SpeechRate { get; set; }
    }
}


