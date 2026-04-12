namespace Imate.API.Presentation.ResponseModels.Speech
{
    public class SynthesizedSpeechResult
    {
        public required string Text { get; init; }
        public required string AudioUrl { get; init; }
        public string? AudioBase64 { get; init; }
        public required string Voice { get; init; }
        public required string Language { get; init; }
    }
}


