using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imate.API.Business.Services.ExternalServices
{
    /// <summary>
    /// Gemini TTS — Chuyển text thành giọng nói bằng gemini-2.5-flash-preview-tts.
    /// Thay thế Azure Speech TTS, sử dụng cùng GeminiSettings API key.
    /// </summary>
    public class GeminiSpeechSynthesisService : ISpeechSynthesisService
    {
        private const int MaxTextLength = 4500;
        private const int CacheExpirationMinutes = 60;

        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _ttsModel;
        private readonly IMemoryCache _cache;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<GeminiSpeechSynthesisService> _logger;

        /// <summary>
        /// Voice mapping: language → Gemini voice name.
        /// Xem danh sách: https://ai.google.dev/gemini-api/docs/speech-generation
        /// </summary>
        private static readonly Dictionary<string, string> VoiceMap = new(StringComparer.OrdinalIgnoreCase)
        {
            // Tiếng Việt
            ["vi-VN"] = "Aoede",
            ["vi"] = "Aoede",
            // Tiếng Anh
            ["en-US"] = "Kore",
            ["en-GB"] = "Kore",
            ["en"] = "Kore",
        };

        private const string DefaultVoice = "Aoede";

        public GeminiSpeechSynthesisService(
            IConfiguration configuration,
            IMemoryCache cache,
            IHttpClientFactory httpClientFactory,
            ILogger<GeminiSpeechSynthesisService> logger)
        {
            _apiKey = configuration["GeminiSettings:ApiKey"]
                ?? throw new InvalidOperationException("GeminiSettings:ApiKey is not configured.");

            var apiUrl = configuration["GeminiSettings:ApiUrl"]
                ?? throw new InvalidOperationException("GeminiSettings:ApiUrl is not configured.");

            // Trích base URL: bỏ phần /models/... ở cuối
            // "https://api.key4u.shop/v1beta/models/gemini-2.5-pro:generateContent"
            // → "https://api.key4u.shop/v1beta"
            var modelsIndex = apiUrl.IndexOf("/models/", StringComparison.OrdinalIgnoreCase);
            _baseUrl = modelsIndex >= 0 ? apiUrl[..modelsIndex] : apiUrl.TrimEnd('/');

            _ttsModel = configuration["GeminiSettings:TtsModel"] ?? "gemini-2.5-flash-preview-tts";
            _cache = cache;
            _httpClientFactory = httpClientFactory;
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

            var targetLanguage = language ?? "vi-VN";
            var targetVoice = voice ?? ResolveVoice(targetLanguage);
            var rate = speechRate ?? 1.0;

            // Kiểm tra cache
            var cacheKey = GenerateCacheKey(normalizedText, targetLanguage, targetVoice, rate) + "_gemini";
            if (_cache.TryGetValue<SynthesizedSpeechResult>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Using cached Gemini TTS audio. CacheKey: {CacheKey}", cacheKey);
                return cachedResult!;
            }

            _logger.LogInformation("Gemini TTS request. Language: {Language}, Voice: {Voice}, Length: {Length}, Rate: {Rate}",
                targetLanguage, targetVoice, normalizedText.Length, rate);

            // Xây dựng request body
            var requestBody = BuildRequestBody(normalizedText, targetVoice);

            // Gọi Gemini TTS API
            var url = $"{_baseUrl}/models/{_ttsModel}:generateContent?key={_apiKey}";
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(120);

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.PostAsync(url, jsonContent, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini TTS API failed. Status: {Status}, Body: {Body}",
                    response.StatusCode, responseJson);
                throw new InvalidOperationException($"Gemini TTS API failed ({response.StatusCode}): {responseJson}");
            }

            // Parse response → lấy audio base64 + mimeType
            var (audioBase64, mimeType) = ExtractAudioData(responseJson);

            // Gemini TTS trả về raw PCM (audio/L16) — browser không phát được
            // Cần wrap thành WAV container
            if (mimeType.Contains("L16", StringComparison.OrdinalIgnoreCase)
                || mimeType.Contains("pcm", StringComparison.OrdinalIgnoreCase))
            {
                // Parse sample rate từ mimeType, ví dụ: "audio/L16;rate=24000"
                var sampleRate = 24000;
                var rateMatch = Regex.Match(mimeType, @"rate=(\d+)");
                if (rateMatch.Success) sampleRate = int.Parse(rateMatch.Groups[1].Value);

                var pcmBytes = Convert.FromBase64String(audioBase64);
                var wavBytes = ConvertPcmToWav(pcmBytes, sampleRate, 1, 16);
                audioBase64 = Convert.ToBase64String(wavBytes);
                mimeType = "audio/wav";

                _logger.LogInformation("Converted PCM → WAV. PCM: {PcmSize} bytes, WAV: {WavSize} bytes, SampleRate: {Rate}",
                    pcmBytes.Length, wavBytes.Length, sampleRate);
            }

            var speechResult = new SynthesizedSpeechResult
            {
                Text = normalizedText,
                AudioUrl = string.Empty,
                AudioBase64 = audioBase64,
                MimeType = mimeType,
                Voice = targetVoice,
                Language = targetLanguage
            };

            _cache.Set(cacheKey, speechResult, TimeSpan.FromMinutes(CacheExpirationMinutes));

            _logger.LogInformation("Gemini TTS completed. Audio base64 length: {Length}, MimeType: {MimeType}", audioBase64.Length, mimeType);
            return speechResult;
        }

        // ── Private helpers ──

        private static object BuildRequestBody(string text, string voiceName)
        {
            return new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text }
                        }
                    }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "AUDIO" },
                    speechConfig = new
                    {
                        voiceConfig = new
                        {
                            prebuiltVoiceConfig = new
                            {
                                voiceName
                            }
                        }
                    }
                }
            };
        }

        private (string audioBase64, string mimeType) ExtractAudioData(string responseJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                var root = doc.RootElement;

                // Response format:
                // { "candidates": [{ "content": { "parts": [{ "inlineData": { "mimeType": "audio/...", "data": "base64..." } }] } }] }
                var candidates = root.GetProperty("candidates");
                var firstCandidate = candidates[0];
                var content = firstCandidate.GetProperty("content");
                var parts = content.GetProperty("parts");
                var firstPart = parts[0];
                var inlineData = firstPart.GetProperty("inlineData");

                var audioBase64 = inlineData.GetProperty("data").GetString()
                    ?? throw new InvalidOperationException("Audio data is null in Gemini TTS response.");
                var mimeType = inlineData.TryGetProperty("mimeType", out var mt)
                    ? mt.GetString() ?? "audio/wav"
                    : "audio/wav";

                _logger.LogInformation("Gemini TTS mimeType: {MimeType}", mimeType);
                return (audioBase64, mimeType);
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                _logger.LogError(ex, "Failed to parse Gemini TTS response: {Response}", responseJson[..Math.Min(500, responseJson.Length)]);
                throw new InvalidOperationException($"Failed to parse Gemini TTS response: {ex.Message}");
            }
        }

        private static string ResolveVoice(string language)
        {
            if (VoiceMap.TryGetValue(language, out var voice))
                return voice;

            // Thử match prefix: "vi-VN" → "vi"
            var prefix = language.Split('-')[0];
            if (VoiceMap.TryGetValue(prefix, out voice))
                return voice;

            return DefaultVoice;
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
        /// <summary>
        /// Wrap raw PCM bytes thành WAV container (thêm 44-byte WAV header).
        /// Browser có thể phát audio/wav nhưng không phát được audio/L16 (raw PCM).
        /// </summary>
        private static byte[] ConvertPcmToWav(byte[] pcmData, int sampleRate, int channels, int bitsPerSample)
        {
            var byteRate = sampleRate * channels * bitsPerSample / 8;
            var blockAlign = channels * bitsPerSample / 8;

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // RIFF header
            writer.Write("RIFF"u8);
            writer.Write(36 + pcmData.Length);        // ChunkSize
            writer.Write("WAVE"u8);

            // fmt sub-chunk
            writer.Write("fmt "u8);
            writer.Write(16);                          // SubChunk1Size (PCM = 16)
            writer.Write((short)1);                    // AudioFormat (PCM = 1)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            // data sub-chunk
            writer.Write("data"u8);
            writer.Write(pcmData.Length);
            writer.Write(pcmData);

            return ms.ToArray();
        }
    }
}
