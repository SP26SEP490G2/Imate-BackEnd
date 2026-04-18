using Imate.AI.Module.Core.Interfaces;
using Imate.API.Business.Interfaces.ExternalServices;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Imate.API.Presentation.ResponseModels.Speech;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Imate.AI.Module.Models.Responses;
using SdkSpeechSynthesisResult = Microsoft.CognitiveServices.Speech.SpeechSynthesisResult;

namespace Imate.API.Business.Services.ExternalServices
{
    public class AzureSpeechSynthesisService : IAzureSpeechSynthesisService
    {
        private const int MaxTextLength = 4500;
        private const int CacheExpirationMinutes = 60;
        private readonly string _subscriptionKey;
        private readonly string _region;
        private readonly string? _endpoint;
        private readonly string _defaultLanguage;
        private readonly string _defaultVoice;
        private readonly string _fallbackVoice;
        private readonly IAwsS3StorageService _storageService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<AzureSpeechSynthesisService> _logger;

        public AzureSpeechSynthesisService(
            IConfiguration configuration,
            IAwsS3StorageService storageService,
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
            _storageService = storageService;
            _cache = cache;
            _logger = logger;
        }

        public async Task<AzureSynthesizedSpeechResult> SynthesizeAsync(
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
                throw new ArgumentException($"Text is too long ({normalizedText.Length} characters). Maximum is {MaxTextLength}.");

            var targetLanguage = language ?? _defaultLanguage;
            var targetVoice = voice ?? ResolveVoice(targetLanguage);
            var rate = speechRate ?? 1.0;

            var cacheKey = GenerateCacheKey(normalizedText, targetLanguage, targetVoice, rate);
            if (_cache.TryGetValue<AzureSynthesizedSpeechResult>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Using cached audio. CacheKey: {CacheKey}", cacheKey);
                return cachedResult!;
            }

            var speechConfig = CreateSpeechConfig(targetLanguage, targetVoice);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);

            _logger.LogInformation("Synthesizing with Azure Speech. Language: {Language}, Voice: {Voice}, Length: {Length}, Rate: {Rate}",
                targetLanguage, targetVoice, normalizedText.Length, rate);

            using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig: null);
            using var registration = cancellationToken.Register(() => synthesizer.StopSpeakingAsync());

            var ssml = BuildSsml(normalizedText, targetLanguage, targetVoice, rate);
            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                var audioBytes = ExtractAudioBytes(result);
                var fileName = $"ai-interview-{DateTime.UtcNow:yyyyMMddHHmmssfff}.mp3";
                var audioUrl = await _storageService.UploadBytesAsync(audioBytes, "audio/mpeg", "ai-interview/tts", fileName);

                _logger.LogInformation("Synthesis completed. Size: {Size} bytes, Url: {Url}", audioBytes.Length, audioUrl);

                var speechResult = new AzureSynthesizedSpeechResult
                {
                    Text = normalizedText,
                    AudioUrl = audioUrl,
                    Voice = targetVoice,
                    Language = targetLanguage
                };

                _cache.Set(cacheKey, speechResult, TimeSpan.FromMinutes(CacheExpirationMinutes));
                return speechResult;
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("Synthesis canceled. Reason: {Reason}, Error: {Error}", cancellation.Reason, cancellation.ErrorDetails);
                throw new InvalidOperationException($"Speech synthesis canceled: {cancellation.ErrorDetails}");
            }

            throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
        }

        public async Task<AzureSynthesizedSpeechResult> SynthesizeToBase64Async(
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
                throw new ArgumentException($"Text is too long ({normalizedText.Length} characters). Maximum is {MaxTextLength}.");

            var targetLanguage = language ?? _defaultLanguage;
            var targetVoice = voice ?? ResolveVoice(targetLanguage);
            var rate = speechRate ?? 1.0;

            var cacheKey = GenerateCacheKey(normalizedText, targetLanguage, targetVoice, rate) + "_base64";
            if (_cache.TryGetValue<AzureSynthesizedSpeechResult>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Using cached base64 audio. CacheKey: {CacheKey}", cacheKey);
                return cachedResult!;
            }

            var speechConfig = CreateSpeechConfig(targetLanguage, targetVoice);
            speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Audio16Khz128KBitRateMonoMp3);

            _logger.LogInformation("Synthesizing to base64. Language: {Language}, Voice: {Voice}, Length: {Length}, Rate: {Rate}",
                targetLanguage, targetVoice, normalizedText.Length, rate);

            using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig: null);
            using var registration = cancellationToken.Register(() => synthesizer.StopSpeakingAsync());

            var ssml = BuildSsml(normalizedText, targetLanguage, targetVoice, rate);
            var result = await synthesizer.SpeakSsmlAsync(ssml);

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                var audioBytes = ExtractAudioBytes(result);
                var audioBase64 = Convert.ToBase64String(audioBytes);

                _logger.LogInformation("Synthesis to base64 completed. Size: {Size} bytes", audioBytes.Length);

                var speechResult = new AzureSynthesizedSpeechResult
                {
                    Text = normalizedText,
                    AudioUrl = string.Empty,
                    AudioBase64 = audioBase64,
                    MimeType = "audio/mp3",
                    Voice = targetVoice,
                    Language = targetLanguage
                };

                _cache.Set(cacheKey, speechResult, TimeSpan.FromMinutes(CacheExpirationMinutes));
                return speechResult;
            }

            if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError("Synthesis canceled. Reason: {Reason}, Error: {Error}", cancellation.Reason, cancellation.ErrorDetails);
                throw new InvalidOperationException($"Speech synthesis canceled: {cancellation.ErrorDetails}");
            }

            throw new InvalidOperationException($"Speech synthesis failed: {result.Reason}");
        }

        /// <summary>
        /// Build SSML với:
        /// - Thay IMATE → phoneme IPA "aɪ mɛt" trước khi escape
        /// - mstts:autolangdetect: Azure tự detect tiếng Anh/Việt
        /// - prosody rate="1.05" pitch="+2%": nhanh hơn mặc định 1 chút, nghe tự nhiên hơn
        /// </summary>
        private static string BuildSsml(string text, string language, string voice, double rate = 1.0)
        {
            var escapedText = text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");

            // Fix đọc sai
            escapedText = Regex.Replace(escapedText, @"\bIMATE\b", "ai mết", RegexOptions.IgnoreCase);
            escapedText = Regex.Replace(escapedText, @"\bim\s*ai\b", "am ây ai", RegexOptions.IgnoreCase);

            // Xuống dòng → pause
            escapedText = escapedText.Replace("\\n", "\n").Replace("\n", "<break time=\"600ms\"/>");

            // Pause thông minh (không phá URL / số)
            escapedText = Regex.Replace(escapedText, @"\.(?=\s+[A-ZÀ-Ỹ])", ".<break time=\"500ms\"/>");
            escapedText = Regex.Replace(escapedText, @",(?!\d)", ",<break time=\"150ms\"/>");

            var targetRate = Math.Abs(rate - 1.0) < 0.01 ? 1.0 : Math.Max(0.5, Math.Min(2.0, rate));
            var prosodyRate = targetRate == 1.0 ? "" : $"rate=\"{targetRate:F2}\"";

            return $@"<speak version=""1.0""
        xmlns=""http://www.w3.org/2001/10/synthesis""
        xmlns:mstts=""http://www.w3.org/2001/mstts""
        xml:lang=""{language}"">
      <voice name=""{voice}"">
        <mstts:autolangdetect onDefaultFail=""true"" />
        <mstts:express-as style=""customerservice"" styledegree=""1.1"">
          <prosody {prosodyRate} pitch=""+2%"">
            {escapedText}
          </prosody>
        </mstts:express-as>
      </voice>
    </speak>";
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

        private static byte[] ExtractAudioBytes(SdkSpeechSynthesisResult result)
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
            if (language.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                return _fallbackVoice;
            return _defaultVoice;
        }

        private static string NormalizeText(string text)
        {
            var withoutLinks = Regex.Replace(text, @"\[(.*?)\]\(.*?\)", "$1");
            var withoutMarkdown = Regex.Replace(withoutLinks, @"[*_`>#\-]", " ");
            var collapsedWhitespace = Regex.Replace(withoutMarkdown, @"\s+", " ");
            return collapsedWhitespace.Trim();
        }

        private static string GenerateCacheKey(string text, string language, string voice, double rate = 1.0)
        {
            var keyString = $"{text}|{language}|{voice}|{rate:F2}";
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(keyString));
            return Convert.ToBase64String(hashBytes);
        }
    }
}