namespace Imate.AI.Module.Models.Responses
{
    /// <summary>
    /// Kết quả tổng hợp giọng nói
    /// </summary>
    public class SynthesizedSpeechResult
    {
        public string Text { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty;
        public string? AudioBase64 { get; set; }
        public string? MimeType { get; set; }
        public string Voice { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}
