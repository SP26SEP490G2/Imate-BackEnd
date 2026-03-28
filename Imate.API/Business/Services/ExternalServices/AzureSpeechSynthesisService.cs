using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imate.API.Business.Services.ExternalServices
{
    /// <summary>
    /// Azure Speech TTS — Chuyển text AI thành giọng nói tự nhiên.
    /// Giọng mặc định: vi-VN-HoaiMyNeural (giọng nữ Việt Nam)
    /// </summary>
    public class AzureSpeechSynthesisService : ISpeechSynthesisService
    {
        private const int MaxTextLength = 4500;
        private const int CacheExpirationMinutes = 60;
        private readonly string _subscriptionKey;
        private readonly string _region;
        private readonly string? _endpoint;
        private readonly string _defaultLanguage;
        private readonly string _defaultVoice;
        private readonly string _fallbackVoice;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AzureSpeechSynthesisService> _logger;

        public AzureSpeechSynthesisService(
            IConfiguration configuration,
            IMemoryCache cache,
            ILogger<AzureSpeechSynthesisService> logger)
        {
            _subscriptionKey = configuration["AzureSpeech:SubscriptionKey"]
                ?? throw new InvalidOperationException("AzureSpeech:SubscriptionKey is not configured.");
            _region = configuration["AzureSpeech:Region"]
                ?? throw new InvalidOperationException("AzureSpeech:Region is not configured.");
            _endpoint = configuration["AzureSpeech:Endpoint"];
            _defaultLanguage = configuration["AzureSpeech:DefaultLanguage"] ?? "vi-VN";
            _defaultVoice = configuration["AzureSpeech:DefaultVoice"] ?? "vi-VN-HoaiMyNeural";
            _fallbackVoice = configuration["AzureSpeech:FallbackVoice"] ?? "en-US-AriaNeural";
            _cache = cache;
            _logger = logger;
        }

        public async Task<SynthesizedSpeechResult> SynthesizeToBase64Async(
            string text,
            string? language = null,
            string? voice = null,
            double? speechRate = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text to synthesize cannot be empty.", nameof(text));

            var normalizedText = NormalizeText(text);
            if (normalizedText.Length > MaxTextLength)
                throw new ArgumentException($"Text quá dài ({normalizedText.Length} ký tự). Tối đa {MaxTextLength} ký tự.");

            var targetLanguage = language ?? _defaultLanguage;
            var targetVoice = voice ?? ResolveVoice(targetLanguage);
            var rate = speechRate ?? 1.0;

            // Kiểm tra cache
            var cacheKey = GenerateCacheKey(normalizedText, targetLanguage, targetVoice, rate) + "_base64";
            if (_cache.TryGetValue<SynthesizedSpeechResult>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Using cached TTS audio. CacheKey: {CacheKey}", cacheKey);
                return cachedResult!;
            }

            var speechConfig = CreateSpeechConfig(targetLanguage, targetVoice);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);

            _logger.LogInformation("Synthesizing TTS. Language: {Language}, Voice: {Voice}, Length: {Length}, Rate: {Rate}",
                targetLanguage, targetVoice, normalizedText.Length, rate);

            using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig: null);
            using var registration = cancellationToken.Register(() => synthesizer.StopSpeakingAsync());

            SpeechSynthesisResult result;
            if (Math.Abs(rate - 1.0) > 0.01)
            {
                var ssml = CreateSsmlWithRate(normalizedText, targetLanguage, targetVoice, rate);
                result = await synthesizer.SpeakSsmlAsync(ssml);
            }
            else
            {
                result = await synthesizer.SpeakTextAsync(normalizedText);
            }

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                var audioBytes = ExtractAudioBytes(result);
                var audioBase64 = Convert.ToBase64String(audioBytes);

                _logger.LogInformation("TTS completed. Audio size: {Size} bytes", audioBytes.Length);

                var speechResult = new SynthesizedSpeechResult
                {
                    Text = normalizedText,
                    AudioUrl = string.Empty,
                    AudioBase64 = audioBase64,
                    Voice = targetVoice,
                    Language = targetLanguage
                };

                _cache.Set(cacheKey, speechResult, TimeSpan.FromMinutes(CacheExpirationMinutes));
                return speechResult;
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("TTS canceled. Reason: {Reason}, Error: {Details}",
                    cancellation.Reason, cancellation.ErrorDetails);
                throw new InvalidOperationException($"TTS canceled: {cancellation.ErrorDetails}");
            }

            throw new InvalidOperationException($"TTS failed: {result.Reason}");
        }

        private SpeechConfig CreateSpeechConfig(string language, string voice)
        {
            SpeechConfig speechConfig;
            if (!string.IsNullOrWhiteSpace(_endpoint))
                speechConfig = SpeechConfig.FromEndpoint(new Uri(_endpoint), _subscriptionKey);
            else
                speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);

            speechConfig.SpeechSynthesisLanguage = language;
            speechConfig.SpeechSynthesisVoiceName = voice;
            return speechConfig;
        }

        private static byte[] ExtractAudioBytes(SpeechSynthesisResult result)
        {
            using var audioStream = AudioDataStream.FromResult(result);
            using var memoryStream = new MemoryStream();
            var buffer = new byte[8192];
            uint bytesRead;
            while ((bytesRead = audioStream.ReadData(buffer)) > 0)
                memoryStream.Write(buffer, 0, (int)bytesRead);
            return memoryStream.ToArray();
        }

        private string ResolveVoice(string language)
        {
            return language.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                ? _fallbackVoice
                : _defaultVoice;
        }

        private static string NormalizeText(string text)
        {
            var withoutLinks = Regex.Replace(text, @"\[(.*?)\]\(.*?\)", "$1");
            var withoutMarkdown = Regex.Replace(withoutLinks, @"[*_`>#\-]", " ");
            var collapsedWhitespace = Regex.Replace(withoutMarkdown, @"\s+", " ");
            return collapsedWhitespace.Trim();
        }

        private static string GenerateCacheKey(string text, string language, string voice, double rate)
        {
            var keyString = $"{text}|{language}|{voice}|{rate:F2}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
            return Convert.ToBase64String(hashBytes);
        }

        private static string CreateSsmlWithRate(string text, string language, string voice, double rate)
        {
            var escapedText = text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");

            var clampedRate = Math.Max(0.5, Math.Min(2.0, rate));

            return $@"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xml:lang=""{language}"">
    <voice name=""{voice}"">
        <prosody rate=""{clampedRate:F2}"">
            {escapedText}
        </prosody>
    </voice>
</speak>";
        }
    }
}
