using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;


namespace Imate.AI.Module.Core.Services
{
    /// <summary>
    /// Gemini AI Service (Tầng 4 - AI Services)
    /// Gọi qua Beeknoee OpenAI-compatible API
    /// </summary>
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiService> _logger;

        // ===== Beeknoee API config =====
        private readonly string _beeknoeeApiUrl;
        private readonly string _beeknoeeApiKey;
        private readonly string _beeknoeeModel;
        private readonly double _temperature;
        private readonly int _maxTokens;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var settings = configuration.GetSection("GeminiSettings");

            _beeknoeeApiUrl = settings["BeeknoeeApiUrl"] ?? "https://platform.beeknoee.com/api/v1/chat/completions";
            _beeknoeeApiKey = settings["BeeknoeeApiKey"] ?? "sk-bee-163ac7606c7e46db8cfd15087fdc4b12";
            _beeknoeeModel = settings["BeeknoeeModel"] ?? "gemini-3-flash";
            _temperature = double.TryParse(settings["Temperature"], out var temp) ? temp : 0.7;
            _maxTokens = int.TryParse(settings["MaxTokens"], out var maxTok) ? maxTok : 8192;
        }

        /// <summary>
        /// Gọi Beeknoee API (OpenAI-compatible) với system prompt và user prompt.
        /// Retry logic: nếu bị rate-limit (429) hoặc server error (5xx),
        /// chờ 30 giây rồi thử lại, tối đa 3 lần.
        /// </summary>
        public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
        {
            const int maxRetries = 3;
            const int retryDelaySeconds = 30;

            var requestBody = new
            {
                model = _beeknoeeModel,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = _temperature,
                max_tokens = _maxTokens,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Post, _beeknoeeApiUrl);
                request.Content = content;
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _beeknoeeApiKey);

                _logger.LogInformation("Calling Beeknoee API (attempt {Attempt}/{Max})...", attempt, maxRetries);
                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var isRetryable = statusCode == 429 || statusCode >= 500;

                    _logger.LogWarning(
                        "Beeknoee API error {StatusCode} (attempt {Attempt}/{Max}): {Body}",
                        response.StatusCode, attempt, maxRetries, responseBody);

                    if (isRetryable && attempt < maxRetries)
                    {
                        _logger.LogInformation(
                            "Rate-limit or server error. Retrying in {Delay}s...", retryDelaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                        continue;
                    }

                    _logger.LogError("Beeknoee API failed after {Attempt} attempt(s). Giving up.", attempt);
                    throw new Exception($"Beeknoee API error: {response.StatusCode}");
                }

                return ParseBeeknoeeResponse(responseBody);
            }

            throw new Exception("Beeknoee API: max retries exhausted");
        }

        /// <summary>
        /// Parse response JSON từ Beeknoee API (OpenAI-compatible format).
        /// Format: { choices: [{ message: { content: "..." } }] }
        /// </summary>
        private string ParseBeeknoeeResponse(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
            {
                throw new Exception("Không nhận được phản hồi từ Beeknoee API (no choices)");
            }

            var firstChoice = choices[0];
            if (!firstChoice.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("content", out var contentEl))
            {
                throw new Exception("Không nhận được phản hồi từ Beeknoee API (no message content)");
            }

            var resultText = contentEl.GetString();

            if (string.IsNullOrEmpty(resultText))
            {
                throw new Exception("Không nhận được phản hồi từ Beeknoee AI");
            }

            _logger.LogInformation("Beeknoee API response received ({Length} chars)", resultText.Length);
            return resultText;
        }

        public async Task<string> GenerateContentForCommentAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            const int retryDelaySeconds = 30;

            var requestBody = new
            {
                model = _beeknoeeModel,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                temperature = _temperature,
                max_tokens = _maxTokens,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);

            _logger.LogInformation("Calling Beeknoee API for Comment...");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));
            try
            {
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    using var request = new HttpRequestMessage(HttpMethod.Post, _beeknoeeApiUrl);
                    request.Content = content;
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _beeknoeeApiKey);

                    _logger.LogInformation("Beeknoee Comment API (attempt {Attempt}/{Max})...", attempt, maxRetries);
                    var response = await _httpClient.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        var statusCode = (int)response.StatusCode;
                        var isRetryable = statusCode == 429 || statusCode >= 500;

                        _logger.LogWarning(
                            "Beeknoee Comment API error {StatusCode} (attempt {Attempt}/{Max}): {Body}",
                            response.StatusCode, attempt, maxRetries, responseBody);

                        if (isRetryable && attempt < maxRetries)
                        {
                            _logger.LogInformation("Retrying in {Delay}s...", retryDelaySeconds);
                            await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                            continue;
                        }

                        throw new Exception($"Beeknoee API error: {response.StatusCode}");
                    }

                    return ParseBeeknoeeResponse(responseBody);
                }

                throw new Exception("Beeknoee API: max retries exhausted");
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Beeknoee API bị timeout hoặc bị hủy bởi người dùng.");
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
                var errorMsg = $"Không thể parse JSON response từ Beeknoee API";
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
    }
}