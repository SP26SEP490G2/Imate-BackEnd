using System.ComponentModel.DataAnnotations;

namespace Imate.AI.Module.Models.Requests
{
    /// <summary>
    /// Yêu cầu tổng hợp giọng nói từ text
    /// </summary>
    public class SynthesizeSpeechRequest
    {
        [Required]
        [StringLength(4500)]
        public string Text { get; set; } = string.Empty;

        /// <summary>Ngôn ngữ, mặc định vi-VN</summary>
        public string? Language { get; set; }

        /// <summary>Tên giọng, mặc định vi-VN-HoaiMyNeural</summary>
        public string? Voice { get; set; }

        /// <summary>Tốc độ nói (0.5 - 2.0), mặc định 1.0</summary>
        public double? SpeechRate { get; set; }

        /// <summary>true = trả base64, false = trả URL</summary>
        public bool ReturnBase64 { get; set; } = true;
    }
}
