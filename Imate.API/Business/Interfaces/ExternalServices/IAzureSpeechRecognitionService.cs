namespace Imate.API.Business.Interfaces.ExternalServices
{
    /// <summary>
    /// Interface for Speech Recognition Service
    /// Supports converting audio to text for both Vietnamese and English
    /// </summary>
    public interface IAzureSpeechRecognitionService
    {
        /// <summary>
        /// Converts audio data (WAV format) to text
        /// </summary>
        /// <param name="audioData">Audio data as byte array (WAV format)</param>
        /// <param name="language">Language code (e.g., "vi-VN" for Vietnamese, "en-US" for English)</param>
        /// <returns>Transcribed text</returns>
        Task<string> RecognizeSpeechAsync(byte[] audioData, string language = "vi-VN");

        /// <summary>
        /// Converts audio stream to text
        /// </summary>
        /// <param name="audioStream">Audio stream (WAV format)</param>
        /// <param name="language">Language code (e.g., "vi-VN" for Vietnamese, "en-US" for English)</param>
        /// <returns>Transcribed text</returns>
        Task<string> RecognizeSpeechFromStreamAsync(Stream audioStream, string language = "vi-VN");
    }
}

