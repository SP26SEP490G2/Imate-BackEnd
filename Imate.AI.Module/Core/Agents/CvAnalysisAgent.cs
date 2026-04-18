using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Agents
{
    /// <summary>
    /// Agent phân tích CV (Tầng 3 - Agents)
    /// Chịu trách nhiệm: load system prompt, build prompt, gọi AI, parse response
    /// Không truy cập data layer — chỉ nhận cvText thuần túy
    /// </summary>
    public class CvAnalysisAgent : ICvAnalysisAgent
    {
        private readonly IGeminiService _geminiService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CvAnalysisAgent> _logger;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public CvAnalysisAgent(
            IGeminiService geminiService,
            IWebHostEnvironment env,
            ILogger<CvAnalysisAgent> logger)
        {
            _geminiService = geminiService;
            _env = env;
            _logger = logger;
        }

        public async Task<CvAnalysisResponse> AnalyseCvAsync(string cvText)
        {
            // 1. Đọc system prompt từ file
            var systemPrompt = await LoadSystemPromptAsync();

            // 2. Gọi Gemini AI
            _logger.LogInformation("=== [CvAnalysis] CV Text to analyze ({Length} chars) ===", cvText.Length);
            _logger.LogInformation("[CvAnalysis] CV Text preview:\n{Preview}", cvText.Substring(0, Math.Min(500, cvText.Length)));
            var userPrompt = $"Hãy phân tích CV sau đây và trả về kết quả dưới dạng JSON:\n\n{cvText}";
            var rawResponse = await _geminiService.GenerateContentAsync(systemPrompt, userPrompt);

            var result = ParseGeminiResponse(rawResponse);
            _logger.LogInformation("CV analysis completed. Score: {Score}, Candidate: {Name}", result.Score, result.CandidateName);

            return result;
        }

        // ── Private helpers ──

        /// <summary>
        /// Đọc system prompt từ file SystemMessages/analyse-cv-system.txt
        /// Tìm file dựa trên vị trí assembly của AI module
        /// </summary>
        private async Task<string> LoadSystemPromptAsync()
        {
            // Tìm thư mục gốc của AI module dựa trên assembly location
            var assemblyDir = Path.GetDirectoryName(typeof(CvAnalysisAgent).Assembly.Location)!;
            var filePath = Path.Combine(assemblyDir, "SystemMessages", "analyse-cv-system.txt");

            // Fallback: tìm trong ContentRootPath (cho development)
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(_env.ContentRootPath, "..", "Imate.AI.Module", "SystemMessages", "analyse-cv-system.txt");
            }

            // Fallback 2: tìm trực tiếp trong ContentRootPath
            if (!File.Exists(filePath))
            {
                filePath = Path.Combine(_env.ContentRootPath, "SystemMessages", "analyse-cv-system.txt");
            }

            if (!File.Exists(filePath))
            {
                _logger.LogError("System prompt file not found. Searched paths include assembly dir and ContentRootPath");
                throw new FileNotFoundException($"System prompt file not found. Hãy tạo file SystemMessages/analyse-cv-system.txt trong Imate.AI.Module");
            }

            var content = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("System prompt file is empty: " + filePath);
            }

            _logger.LogInformation("Loaded system prompt from file: {Path}", filePath);
            return content;
        }

        private CvAnalysisResponse ParseGeminiResponse(string responseText)
        {
            var cleaned = Regex.Replace(responseText.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "");
            cleaned = cleaned.Trim();

            try
            {
                var result = JsonSerializer.Deserialize<CvAnalysisResponse>(cleaned, JsonOptions);
                if (result == null)
                    throw new Exception("Parsed result is null");
                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini response: {Response}", cleaned.Substring(0, Math.Min(500, cleaned.Length)));
                throw new Exception("Không thể phân tích phản hồi từ AI. Vui lòng thử lại.", ex);
            }
        }
    }
}