using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Imate.AI.Module.Models.Responses;
using Microsoft.Extensions.Logging;


namespace Imate.AI.Module.Core.Orchestrators
{
    /// <summary>
    /// Orchestrator phân tích CV (Tầng 2 - Orchestrators)
    /// Điều phối workflow: cache check → data access → Agent → save cache
    /// </summary>
    public class CvAnalysisOrchestrator : ICvAnalysisOrchestrator
    {
        private readonly ICvAnalysisAgent _cvAnalysisAgent;
        private readonly ICvDataProvider? _cvDataProvider;
        private readonly ILogger<CvAnalysisOrchestrator> _logger;

        public CvAnalysisOrchestrator(
            ICvAnalysisAgent cvAnalysisAgent,
            ILogger<CvAnalysisOrchestrator> logger,
            ICvDataProvider? cvDataProvider = null)
        {
            _cvAnalysisAgent = cvAnalysisAgent;
            _logger = logger;
            _cvDataProvider = cvDataProvider;
        }

        public async Task<CvAnalysisResponse> AnalyseCvAsync(int accountId, AnalyseCvRequest request)
        {
            // 1. Check cache trước (chỉ khi có cvId và không force reanalyze)
            if (request.CvId.HasValue && _cvDataProvider != null && !request.ForceReanalyze)
            {
                var cached = await _cvDataProvider.GetCachedAnalysisAsync(accountId, request.CvId.Value);
                if (!string.IsNullOrWhiteSpace(cached))
                {
                    _logger.LogInformation("Returning cached CV analysis for account {AccountId}, cvId {CvId}", accountId, request.CvId.Value);
                    // Parse cached result through agent's format
                    return System.Text.Json.JsonSerializer.Deserialize<CvAnalysisResponse>(cached, 
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? throw new Exception("Cannot parse cached analysis");
                }
            }

            // 2. Nếu force reanalyze, xóa cả ScannedData để re-extract từ file gốc
            if (request.ForceReanalyze && request.CvId.HasValue && _cvDataProvider != null)
            {
                await _cvDataProvider.ClearScannedDataAsync(accountId, request.CvId.Value);
            }

            // 3. Lấy CV text
            string cvText = await GetCvTextAsync(accountId, request);

            if (string.IsNullOrWhiteSpace(cvText))
            {
                throw new ArgumentException("Không có nội dung CV để phân tích. Vui lòng cung cấp CvId hoặc CvText.");
            }

            // 4. Gọi Agent phân tích
            var result = await _cvAnalysisAgent.AnalyseCvAsync(cvText);

            // 5. Lưu cache vào DB (chỉ khi có cvId)
            if (request.CvId.HasValue && _cvDataProvider != null)
            {
                try
                {
                    var rawJson = System.Text.Json.JsonSerializer.Serialize(result);
                    await _cvDataProvider.SaveAnalysisResultAsync(accountId, request.CvId.Value, rawJson);
                    _logger.LogInformation("Cached CV analysis result for cvId {CvId}", request.CvId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cache CV analysis result for cvId {CvId}", request.CvId.Value);
                }
            }

            return result;
        }

        private async Task<string> GetCvTextAsync(int accountId, AnalyseCvRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.CvText))
            {
                return request.CvText;
            }

            if (request.CvId.HasValue)
            {
                if (_cvDataProvider == null)
                    throw new InvalidOperationException("ICvDataProvider chưa được đăng ký. Không thể truy vấn CV từ database.");

                return await _cvDataProvider.GetCvTextAsync(accountId, request.CvId.Value);
            }

            throw new ArgumentException("Vui lòng cung cấp CvId hoặc CvText.");
        }
    }
}