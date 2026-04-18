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

        // Default settings (GeminiSettings)
        private readonly string _beeknoeeApiUrl;
        private readonly string _beeknoeeApiKey;
        private readonly string _beeknoeeModel;
        private readonly double _temperature;
        private readonly int _maxTokens;

        // CV-specific settings (CVGeminiSettings)
        private readonly string _cvApiUrl;
        private readonly string _cvApiKey;
        private readonly string _cvModel;
        private readonly double _cvTemperature;
        private readonly int _cvMaxTokens;

        public GeminiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var settings = configuration.GetSection("GeminiSettings");
            _beeknoeeApiUrl = settings["BeeknoeeApiUrl"] ?? "https://platform.beeknoee.com/api/v1/chat/completions";
            _beeknoeeApiKey = settings["BeeknoeeApiKey"] ?? "";
            _beeknoeeModel = settings["BeeknoeeModel"] ?? "gemini-3-flash";
            _temperature = double.TryParse(settings["Temperature"], out var temp) ? temp : 0.7;
            _maxTokens = int.TryParse(settings["MaxTokens"], out var maxTok) ? maxTok : 8192;

            // Đọc CVGeminiSettings, fallback về GeminiSettings nếu chưa config
            var cvSettings = configuration.GetSection("CVGeminiSettings");
            _cvApiUrl = cvSettings["BeeknoeeApiUrl"] ?? _beeknoeeApiUrl;
            _cvApiKey = cvSettings["BeeknoeeApiKey"] ?? _beeknoeeApiKey;
            _cvModel = cvSettings["BeeknoeeModel"] ?? _beeknoeeModel;
            _cvTemperature = double.TryParse(cvSettings["Temperature"], out var cvTemp) ? cvTemp : 0;
            _cvMaxTokens = int.TryParse(cvSettings["MaxTokens"], out var cvMax) ? cvMax : 8192;
        }

        /// <summary>
        /// Gọi AI với settings mặc định (GeminiSettings) - dùng cho các feature khác
        /// </summary>
        public async Task<string> GenerateContentAsync(string systemPrompt, string userPrompt)
        {
            return await CallBeeknoeeAsync(
                systemPrompt, userPrompt,
                _beeknoeeApiUrl, _beeknoeeApiKey, _beeknoeeModel, _temperature, _maxTokens,
                callerName: "GenerateContent");
        }

        /// <summary>
        /// Gọi AI với CVGeminiSettings (Temperature=0.1) - dùng riêng cho Analyse CV
        /// </summary>
        public async Task<string> GenerateContentForCvAnalysisAsync(string systemPrompt, string userPrompt)
        {
            _logger.LogInformation(
                "[CvAnalysis] Using CVGeminiSettings: Model={Model}, Temp={Temp}",
                _cvModel, _cvTemperature);

            return await CallBeeknoeeAsync(
                systemPrompt, userPrompt,
                _cvApiUrl, _cvApiKey, _cvModel, _cvTemperature, _cvMaxTokens,
                callerName: "CvAnalysis");
        }

        /// <summary>
        /// Gọi AI cho Comment (có CancellationToken + timeout 3s)
        /// </summary>
        public async Task<string> GenerateContentForCommentAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default)
        {
            const int maxRetries = 3;
            const int retryDelaySeconds = 30;

            var jsonContent = BuildRequestJson(_beeknoeeModel, systemPrompt, userPrompt, _temperature, _maxTokens);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(3));

            try
            {
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    using var request = BuildHttpRequest(_beeknoeeApiUrl, _beeknoeeApiKey, jsonContent);

                    _logger.LogInformation("Beeknoee Comment API (attempt {Attempt}/{Max})...", attempt, maxRetries);
                    var response = await _httpClient.SendAsync(request, cts.Token);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        var statusCode = (int)response.StatusCode;
                        _logger.LogWarning("Beeknoee Comment API error {StatusCode} (attempt {Attempt}/{Max}): {Body}",
                            response.StatusCode, attempt, maxRetries, responseBody);

                        if ((statusCode == 429 || statusCode >= 500) && attempt < maxRetries)
                        {
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
                _logger.LogError("Beeknoee API bị timeout hoặc bị hủy.");
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

                var cleanedResponse = responseMessage.Trim();
                if (cleanedResponse.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                    cleanedResponse = cleanedResponse.Substring(7);
                if (cleanedResponse.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                    cleanedResponse = cleanedResponse.Substring(3);
                if (cleanedResponse.EndsWith("```", StringComparison.OrdinalIgnoreCase))
                    cleanedResponse = cleanedResponse.Substring(0, cleanedResponse.Length - 3);
                cleanedResponse = cleanedResponse.Trim();

                var jsonDoc = JsonDocument.Parse(cleanedResponse);
                var root = jsonDoc.RootElement;

                return new CommentModerationResult
                {
                    IsSafe = root.TryGetProperty("is_safe", out var isSafeEl) && isSafeEl.GetBoolean(),
                    ViolationCategory = root.TryGetProperty("violation_category", out var violationCat)
                        ? violationCat.GetString() ?? "None" : "None",
                    Reasoning = root.TryGetProperty("reasoning", out var reasoningEl)
                        ? reasoningEl.GetString() ?? "" : "",
                    SuggestedAction = root.TryGetProperty("suggested_action", out var actionEl)
                        ? actionEl.GetString() ?? "" : ""
                };
            }
            catch (JsonException ex)
            {
                var errorMsg = "Không thể parse JSON response từ Beeknoee API";
                if (!string.IsNullOrEmpty(responseMessage))
                    errorMsg += $". Response gốc: {responseMessage.Substring(0, Math.Min(500, responseMessage.Length))}";
                throw new Exception(errorMsg);
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi kiểm duyệt comment: {ex.Message}", ex);
            }
        }

        // =====================================================================
        // PRIVATE HELPERS - tránh lặp code giữa các method
        // =====================================================================

        private async Task<string> CallBeeknoeeAsync(
            string systemPrompt, string userPrompt,
            string apiUrl, string apiKey, string model, double temperature, int maxTokens,
            string callerName = "Gemini")
        {
            const int maxRetries = 3;
            const int retryDelaySeconds = 30;

            var jsonContent = BuildRequestJson(model, systemPrompt, userPrompt, temperature, maxTokens);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var request = BuildHttpRequest(apiUrl, apiKey, jsonContent);

                _logger.LogInformation("[{Caller}] Calling Beeknoee API (attempt {Attempt}/{Max})...",
                    callerName, attempt, maxRetries);

                var response = await _httpClient.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    _logger.LogWarning("[{Caller}] Beeknoee API error {StatusCode} (attempt {Attempt}/{Max}): {Body}",
                        callerName, response.StatusCode, attempt, maxRetries, responseBody);

                    if ((statusCode == 429 || statusCode >= 500) && attempt < maxRetries)
                    {
                        _logger.LogInformation("[{Caller}] Retrying in {Delay}s...", callerName, retryDelaySeconds);
                        await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                        continue;
                    }

                    _logger.LogError("[{Caller}] Beeknoee API failed after {Attempt} attempt(s). Giving up.", callerName, attempt);
                    throw new Exception($"Beeknoee API error: {response.StatusCode}");
                }

                return ParseBeeknoeeResponse(responseBody);
            }

            throw new Exception("Beeknoee API: max retries exhausted");
        }

        private static string BuildRequestJson(string model, string systemPrompt, string userPrompt, double temperature, int maxTokens)
        {
            return JsonSerializer.Serialize(new
            {
                model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user",   content = userPrompt }
                },
                temperature,
                max_tokens = maxTokens,
                stream = false
            });
        }

        private static HttpRequestMessage BuildHttpRequest(string apiUrl, string apiKey, string jsonContent)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
            request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            return request;
        }

        private string ParseBeeknoeeResponse(string responseBody)
        {
            using var doc = JsonDocument.Parse(responseBody);

            if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                throw new Exception("Không nhận được phản hồi từ Beeknoee API (no choices)");

            var firstChoice = choices[0];
            if (!firstChoice.TryGetProperty("message", out var message) ||
                !message.TryGetProperty("content", out var contentEl))
                throw new Exception("Không nhận được phản hồi từ Beeknoee API (no message content)");

            var resultText = contentEl.GetString();
            if (string.IsNullOrEmpty(resultText))
                throw new Exception("Không nhận được phản hồi từ Beeknoee AI");

            _logger.LogInformation("Beeknoee API response received ({Length} chars)", resultText.Length);
            return resultText;
        }
    }
}