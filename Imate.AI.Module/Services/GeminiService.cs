using System.Text;
using System.Text.Json;
using Imate.AI.Module.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Imate.AI.Module.Services
{
    /// <summary>
    /// Gemini AI Service - gọi Gemini API qua key4u.shop proxy
    /// </summary>
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;
        private readonly string _apiKey;
        private readonly string _apiUrl;
        private readonly double _temperature;
        private readonly double _topP;
        private readonly int _thinkingBudget;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var settings = configuration.GetSection("GeminiSettings");
            _apiKey = settings["ApiKey"] ?? throw new InvalidOperationException("GeminiSettings:ApiKey is required");
            _apiUrl = settings["ApiUrl"] ?? "https://api.key4u.shop/v1beta/models/gemini-2.5-pro:generateContent";
            _temperature = double.TryParse(settings["Temperature"], out var temp) ? temp : 1.0;
            _topP = double.TryParse(settings["TopP"], out var topP) ? topP : 1.0;
            _thinkingBudget = int.TryParse(settings["ThinkingBudget"], out var budget) ? budget : 26240;
        }

        /// <summary>
        /// Gọi Gemini API với system prompt và user prompt
        /// </summary>
        public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
        {
            var requestUrl = $"{_apiUrl}?key={_apiKey}";

            var requestBody = new
            {
                systemInstruction = new
                {
                    parts = new[] { new { text = systemPrompt } }
                },
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[] { new { text = userPrompt } }
                    }
                },
                generationConfig = new
                {
                    temperature = _temperature,
                    topP = _topP,
                    thinkingConfig = new
                    {
                        includeThoughts = true,
                        thinkingBudget = _thinkingBudget
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling Gemini API...");

            var response = await _httpClient.PostAsync(requestUrl, content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error {StatusCode}: {Body}", response.StatusCode, responseBody);
                throw new Exception($"Gemini API error: {response.StatusCode}");
            }

            // Parse response - Gemini 2.5 Pro with thinking trả về nhiều parts
            using var doc = JsonDocument.Parse(responseBody);
            var parts = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            // Tìm part chứa response text (không phải thought)
            string? resultText = null;
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("thought", out var thought) && thought.GetBoolean())
                    continue;

                if (part.TryGetProperty("text", out var text))
                {
                    resultText = text.GetString();
                    break;
                }
            }

            // Fallback: lấy part cuối cùng
            if (string.IsNullOrEmpty(resultText))
            {
                var lastPart = parts[parts.GetArrayLength() - 1];
                resultText = lastPart.GetProperty("text").GetString();
            }

            if (string.IsNullOrEmpty(resultText))
            {
                throw new Exception("Không nhận được phản hồi từ Gemini AI");
            }

            _logger.LogInformation("Gemini API response received ({Length} chars)", resultText.Length);
            return resultText;
        }
    }
}
