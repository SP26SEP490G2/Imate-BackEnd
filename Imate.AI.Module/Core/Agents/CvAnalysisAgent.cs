using System.Text.Json;
using System.Text.RegularExpressions;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Agents
{
    public class CvAnalysisAgent : ICvAnalysisAgent
    {
        private readonly IGeminiService _geminiService;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<CvAnalysisAgent> _logger;

        // Ngưỡng điểm tối đa từng tiêu chí để clamp đề phòng AI trả vượt mức
        private const int MaxWorkExperience = 30;
        private const int MaxSkillsAndTech = 20;
        private const int MaxEducationAndCerts = 15;
        private const int MaxRealWorldProjects = 20;
        private const int MaxCvStructure = 15;

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

            // 2. Gọi Gemini AI với CVGeminiSettings (Temperature=0)
            _logger.LogInformation("=== [CvAnalysis] CV Text to analyze ({Length} chars) ===", cvText.Length);
            _logger.LogInformation("[CvAnalysis] CV Text preview:\n{Preview}", cvText.Substring(0, Math.Min(500, cvText.Length)));

            var userPrompt = $"Hãy phân tích CV sau đây và trả về kết quả dưới dạng JSON:\n\n{cvText}";
            var rawResponse = await _geminiService.GenerateContentForCvAnalysisAsync(systemPrompt, userPrompt);

            // 3. Parse + BE tự tính Score, MarketFit
            var result = ParseAndComputeResponse(rawResponse);

            _logger.LogInformation(
                "CV analysis completed. Score: {Score} (WE={WE}, ST={ST}, EC={EC}, RP={RP}, CS={CS}), MarketFit: {MF}, Level: {Level}, Candidate: {Name}",
                result.Score,
                result.ScoreBreakdown.WorkExperience,
                result.ScoreBreakdown.SkillsAndTech,
                result.ScoreBreakdown.EducationAndCerts,
                result.ScoreBreakdown.RealWorldProjects,
                result.ScoreBreakdown.CvStructure,
                result.MarketFit,
                result.DetectedLevel,
                result.CandidateName);

            return result;
        }
        private CvAnalysisResponse ParseAndComputeResponse(string responseText)
        {
            var cleaned = Regex.Replace(responseText.Trim(), @"^```(?:json)?\s*", "", RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\s*```\s*$", "").Trim();

            GeminiCvRawResponse raw;
            try
            {
                raw = JsonSerializer.Deserialize<GeminiCvRawResponse>(cleaned, JsonOptions)
                    ?? throw new Exception("Parsed result is null");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse Gemini response: {Response}", cleaned.Substring(0, Math.Min(500, cleaned.Length)));
                throw new Exception("Không thể phân tích phản hồi từ AI. Vui lòng thử lại.", ex);
            }

            // Clamp từng điểm về đúng giới hạn đề phòng AI trả vượt mức
            var breakdown = new ScoreBreakdownDto
            {
                WorkExperience = Clamp(raw.ScoreBreakdown?.WorkExperience ?? 0, 0, MaxWorkExperience),
                SkillsAndTech = Clamp(raw.ScoreBreakdown?.SkillsAndTech ?? 0, 0, MaxSkillsAndTech),
                EducationAndCerts = Clamp(raw.ScoreBreakdown?.EducationAndCerts ?? 0, 0, MaxEducationAndCerts),
                RealWorldProjects = Clamp(raw.ScoreBreakdown?.RealWorldProjects ?? 0, 0, MaxRealWorldProjects),
                CvStructure = Clamp(raw.ScoreBreakdown?.CvStructure ?? 0, 0, MaxCvStructure),
            };

            int totalScore = breakdown.WorkExperience
                           + breakdown.SkillsAndTech
                           + breakdown.EducationAndCerts
                           + breakdown.RealWorldProjects
                           + breakdown.CvStructure;

            string marketFit = totalScore >= 70 ? "Cao"
                             : totalScore >= 40 ? "Trung bình"
                             : "Thấp";

            return new CvAnalysisResponse
            {
                Score = totalScore,
                MarketFit = marketFit,
                CandidateName = raw.CandidateName ?? string.Empty,
                JobTitle = raw.JobTitle ?? string.Empty,
                DetectedLevel = raw.DetectedLevel ?? string.Empty,
                ScoreBreakdown = breakdown,
                Strengths = raw.Strengths ?? new(),
                Improvements = raw.Improvements ?? new(),
                InterviewQuestions = raw.InterviewQuestions ?? new(),
            };
        }

        private static int Clamp(int value, int min, int max)
            => value < min ? min : value > max ? max : value;

        private async Task<string> LoadSystemPromptAsync()
        {
            var assemblyDir = Path.GetDirectoryName(typeof(CvAnalysisAgent).Assembly.Location)!;
            var filePath = Path.Combine(assemblyDir, "SystemMessages", "analyse-cv-system.txt");

            if (!File.Exists(filePath))
                filePath = Path.Combine(_env.ContentRootPath, "..", "Imate.AI.Module", "SystemMessages", "analyse-cv-system.txt");

            if (!File.Exists(filePath))
                filePath = Path.Combine(_env.ContentRootPath, "SystemMessages", "analyse-cv-system.txt");

            if (!File.Exists(filePath))
            {
                _logger.LogError("System prompt file not found.");
                throw new FileNotFoundException("System prompt file not found. Hãy tạo file SystemMessages/analyse-cv-system.txt trong Imate.AI.Module");
            }

            var content = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("System prompt file is empty: " + filePath);

            _logger.LogInformation("Loaded system prompt from: {Path}", filePath);
            return content;
        }


        private class GeminiCvRawResponse
        {
            public string? CandidateName { get; set; }
            public string? JobTitle { get; set; }
            public string? DetectedLevel { get; set; }
            public GeminiScoreBreakdown? ScoreBreakdown { get; set; }
            public List<CvInsightDto>? Strengths { get; set; }
            public List<CvInsightDto>? Improvements { get; set; }
            public List<InterviewQuestionDto>? InterviewQuestions { get; set; }
        }

        private class GeminiScoreBreakdown
        {
            public int WorkExperience { get; set; }
            public int SkillsAndTech { get; set; }
            public int EducationAndCerts { get; set; }
            public int RealWorldProjects { get; set; }
            public int CvStructure { get; set; }
        }
    }
}