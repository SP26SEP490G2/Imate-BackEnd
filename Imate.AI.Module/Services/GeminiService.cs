using Imate.AI.Module.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

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

        public async Task<string> GenerateContentForCommentAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
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
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
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
            catch (OperationCanceledException)
            {
                _logger.LogError("API Gemini bị timeout hoặc bị hủy bởi người dùng.");
                throw new Exception("Yêu cầu quá thời gian xử lý, vui lòng thử lại.");
            }
        }

        public async Task<CommentModerationResult> ModerateCommentAsync(string commentContent)
        {
            var systemPrompt = "Bạn là Một AI Mod Cấp Cao (Senior AI Content Moderator) của một diễn đàn cộng đồng lớn. Sứ mệnh của bạn là đảm bảo một môi trường thảo luận an toàn, văn minh và tích cực. Bạn hành động một cách nhất quán, khách quan và không thiên vị.";

            var userPrompt = $@"
## HỆ THỐNG QUY TẮC (GUARDRAILS)

Bạn PHẢI phân loại BẤT KỲ nội dung nào vi phạm MỘT hoặc NHIỀU quy tắc sau là ""unsafe"" (không an toàn).

1. **Ngôn từ tục tĩu & Chửi thề (Profanity):** Bất kỳ từ ngữ thô tục, chửi rủa, báng bổ, hoặc từ viết tắt/viết lách (ví dụ: vcl, dkm,...) nhằm mục đích lăng mạ.

2. **Kích động & Thù hằn (Hate Speech & Incitement):** Nội dung tấn công, phân biệt đối xử hoặc kích động bạo lực/thù hằn nhắm vào một cá nhân hoặc nhóm dựa trên: chủng tộc, tôn giáo, giới tính, khuynh hướng tính dục, khuyết tật, hoặc nguồn gốc quốc gia.

3. **Bạo lực & Đe dọa (Violence & Threats):** Nội dung mô tả bạo lực cực đoan, cổ xúy hành vi bạo lực, hoặc đe dọa gây hại cho người khác hoặc chính bản thân họ.

4. **Từ lóng tiêu cực & Lách luật (Toxic Slang & Evasion):** Các từ lóng, tiếng địa phương hoặc ""teencode"" được sử dụng với ý nghĩa miệt thị, mỉa mai độc hại, hoặc cố tình viết sai chính tả để lách bộ lọc (ví dụ: ""gi*ết"", ""th**ng"").

5. **Quấy rối & Bắt nạt (Harassment & Bullying):** Các bình luận nhằm mục đích làm nhục, chế giễu hoặc đe dọa một cá nhân cụ thể.

6. **Spam & Lừa đảo (Spam & Scams):** Các liên kết lừa đảo, quảng cáo không liên quan, nội dung lặp đi lặp lại.

## NHIỆM VỤ (TASK)

Phân tích **[Comment]** được cung cấp dưới đây. Dựa trên **HỆ THỐNG QUY TẮC** ở trên, hãy trả về kết quả phân loại của bạn.

**Comment cần kiểm duyệt:**
{commentContent}

## ĐỊNH DẠNG ĐẦU RA (STRUCTURED OUTPUT)

Bạn PHẢI trả lời bằng định dạng JSON chính xác sau. KHÔNG thêm bất kỳ lời giải thích hay văn bản nào bên ngoài khối JSON.

```json
{{
  ""is_safe"": <boolean>,
  ""violation_category"": ""<string>"",
  ""reasoning"": ""<string>"",
  ""suggested_action"": ""<string>""
}}
```

**QUY TẮC:**
- Nếu comment AN TOÀN (is_safe = true): violation_category = ""None"", reasoning giải thích ngắn gọn, suggested_action = ""Approve""
- Nếu comment KHÔNG AN TOÀN (is_safe = false): violation_category phải là một trong các loại vi phạm (Profanity, Hate Speech, Violence, Toxic Slang, Harassment, Spam), reasoning giải thích rõ ràng lý do, suggested_action = ""Reject""
- Chỉ trả về JSON hợp lệ, không thêm bất kỳ văn bản giải thích nào khác
- Đảm bảo JSON hợp lệ 100%";

            string? responseMessage = null;
            try
            {
                responseMessage = await GenerateContentForCommentAsync(systemPrompt, userPrompt);

                // Clean response - remove markdown code blocks if present
                var cleanedResponse = responseMessage.Trim();
                if (cleanedResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                {
                    cleanedResponse = cleanedResponse.Substring(7);
                }
                if (cleanedResponse.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                {
                    cleanedResponse = cleanedResponse.Substring(3);
                }
                if (cleanedResponse.EndsWith("```", StringComparison.OrdinalIgnoreCase))
                {
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                }
                cleanedResponse = cleanedResponse.Trim();

                // Parse JSON response
                var jsonDoc = JsonDocument.Parse(cleanedResponse);
                var root = jsonDoc.RootElement;

                var isSafe = root.TryGetProperty("is_safe", out var isSafeEl) && isSafeEl.GetBoolean();
                var violationCategory = root.TryGetProperty("violation_category", out var violationCat)
                    ? violationCat.GetString() ?? "None"
                    : "None";
                var reasoning = root.TryGetProperty("reasoning", out var reasoningEl)
                    ? reasoningEl.GetString() ?? ""
                    : "";
                var suggestedAction = root.TryGetProperty("suggested_action", out var actionEl)
                    ? actionEl.GetString() ?? ""
                    : "";

                var result = new CommentModerationResult
                {
                    IsSafe = isSafe,
                    ViolationCategory = violationCategory,
                    Reasoning = reasoning,
                    SuggestedAction = suggestedAction
                };

                return result;
            }
            catch (JsonException ex)
            {
                var errorMsg = $"Không thể parse JSON response từ Gemini API";
                if (!string.IsNullOrEmpty(responseMessage))
                {
                    errorMsg += $". Response gốc: {responseMessage.Substring(0, Math.Min(500, responseMessage.Length))}";
                }
                throw new Exception(errorMsg);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi kiểm duyệt comment: {ex.Message}", ex);
            }
        }

        // --- BLOOM'S TAXONOMY FRAMEWORK METHODS ---
        // Paper Section 2.2: "LLM Generation Engine and Theoretical Calibration"

        /// <summary>
        /// Generate question aligned with Bloom's Taxonomy level
        /// Paper Section 2.2.1: "Bloom's Taxonomy for Content Depth"
        /// </summary>
    }
}