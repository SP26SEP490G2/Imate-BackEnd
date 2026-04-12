namespace Imate.AI.Module.Models.Responses
{
    public class AzureSynthesizedSpeechResult
    {
        public required string Text { get; init; }
        public required string AudioUrl { get; init; }
        public string? MimeType { get; set; }
        public string? AudioBase64 { get; init; }
        public required string Voice { get; init; }
        public required string Language { get; init; }
    }
}


