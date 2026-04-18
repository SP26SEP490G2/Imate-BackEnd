using Imate.AI.Module.Models.Responses;


namespace Imate.AI.Module.Core.Interfaces
{
    /// <summary>
    /// Interface cho dịch vụ tổng hợp giọng nói (Text-to-Speech)
    /// </summary>
    public interface ISpeechSynthesisService
    {
        /// <summary>
        /// Chuyển text thành audio, trả về base64
        /// </summary>
        Task<SynthesizedSpeechResult> SynthesizeToBase64Async(
            string text,
            string? language = null,
            string? voice = null,
            double? speechRate = null,
            CancellationToken cancellationToken = default);
    }
}