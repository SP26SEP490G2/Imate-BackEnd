using System.Text.Json.Nodes;

namespace Imate.API.Business.Interfaces.ExternalServices
{
	public interface IGeminiAPIService
	{
		Task<JsonNode?> GetCompletionsUsageAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, string groupBy, int limit);

		Task<JsonNode?> GetEmbeddingsUsageAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, string groupBy, int limit);

		Task<JsonNode?> GetImagesUsageAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, string groupBy, int limit);

		Task<JsonNode?> GetCostsAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit);

		Task<JsonNode?> GetUsageByUsersAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit);

		Task<JsonNode?> GetUsageByApiKeysAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit);

		Task<JsonNode?> GetUsageByServicesAsync(DateTimeOffset start, DateTimeOffset end, string bucketWidth, int limit);

		Task<string> DebugRawUsageAsync();

		Task<string> DebugRawCostsAsync();
	}
}
