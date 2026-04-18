using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    public interface IAzureSpeechSynthesisService
    {
        Task<AzureSynthesizedSpeechResult> SynthesizeAsync(
            string text,
            string? language = null,
            string? voice = null,
            double? speechRate = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Synthesize speech and return base64 audio (faster, no S3 upload)
        /// </summary>
        Task<AzureSynthesizedSpeechResult> SynthesizeToBase64Async(
            string text,
            string? language = null,
            string? voice = null,
            double? speechRate = null,
            CancellationToken cancellationToken = default);
    }
}

