using System.Net.Http.Headers;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Imate.API.Business.Interfaces.ExternalServices;
using Imate.API.Infrastructure.Configurations;

namespace Imate.API.Business.Services.ExternalServices
{
    public class GeminiAPIService : IGeminiAPIService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiAdminOptions _options;

        public GeminiAPIService(HttpClient httpClient, IOptions<GeminiAdminOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;

            if (string.IsNullOrWhiteSpace(_options.AdminApiKey))
            {
                throw new InvalidOperationException("Gemini admin API key is not configured.");
            }

            _httpClient.BaseAddress ??= new Uri("https://monitoring.googleapis.com/");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AdminApiKey);
        }

        public async Task<JsonNode?> GetCompletionsUsageAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, string groupBy, int limit)
        {
            // Google Cloud Monitoring equivalent for request/token counts
            var url = BuildUrl(
                $"v3/projects/{_options.ProjectId}/timeSeries",
                start,
                end,
                metric: "vertex_ai.googleapis.com/llm/request_count");
            return await SendGetAsync(url);
        }

        public async Task<JsonNode?> GetEmbeddingsUsageAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, string groupBy, int limit)
        {
            var url = BuildUrl(
                $"v3/projects/{_options.ProjectId}/timeSeries",
                start,
                end,
                metric: "vertex_ai.googleapis.com/llm/embedding_count");
            return await SendGetAsync(url);
        }

        public async Task<JsonNode?> GetImagesUsageAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, string groupBy, int limit)
        {
            // Placeholder for Image generation frequency if applicable
            return null;
        }

        public async Task<JsonNode?> GetCostsAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit)
        {
            // Vertex AI costs are usually fetched via Cloud Billing API or exported to BigQuery
            // This is a placeholder for a specific Billing API call
            var url = $"https://cloudbilling.googleapis.com/v1/billingAccounts/{_options.BillingAccountId}/services";
            return await SendGetAsync(url);
        }

        public async Task<JsonNode?> GetUsageByUsersAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit)
        {
            // Monitoring by user is possible if specifically tagged in metrics
            return null;
        }

        public async Task<JsonNode?> GetUsageByApiKeysAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit)
        {
            return null;
        }

        public async Task<JsonNode?> GetUsageByServicesAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit)
        {
            return null;
        }

        private string BuildUrl(
            string path,
            DateTimeOffset start,
            DateTimeOffset? end = null,
            string? metric = null)
        {
            var query = new Dictionary<string, string?>
            {
                { "filter", $"metric.type=\"{metric}\"" },
                { "interval.startTime", start.ToString("yyyy-MM-ddTHH:mm:ssZ") },
            };

            if (end.HasValue)
            {
                query["interval.endTime"] = end.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            return QueryHelpers.AddQueryString(path, query);
        }

        public async Task<string> DebugRawUsageAsync()
        {
            var now = DateTimeOffset.UtcNow;
            var start = now.AddDays(-30);

            var url = BuildUrl($"v3/projects/{_options.ProjectId}/timeSeries", start, now, "vertex_ai.googleapis.com/llm/request_count");

            using var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            Console.WriteLine("=== Gemini Monitoring API Debug ===");
            Console.WriteLine($"URL: {url}");
            Console.WriteLine($"STATUS: {(int)response.StatusCode} {response.StatusCode}");
            Console.WriteLine("===================================");

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Google API returned {(int)response.StatusCode}. Body: {body}");
            }

            return body;
        }

        public async Task<string> DebugRawCostsAsync()
        {
            var url = $"https://cloudbilling.googleapis.com/v1/billingAccounts/{_options.BillingAccountId}/services";

            using var response = await _httpClient.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Google API returned {(int)response.StatusCode}. Body: {body}");
            }

            return body;
        }

        private async Task<JsonNode?> SendGetAsync(string url)
        {
            using var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Google API returned {(int)response.StatusCode}. Response: {errorContent}");
            }
            
            await using var stream = await response.Content.ReadAsStreamAsync();
            return await JsonNode.ParseAsync(stream);
        }
    }
}

