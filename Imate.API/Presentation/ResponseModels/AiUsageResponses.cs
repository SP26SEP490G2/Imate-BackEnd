using System.Linq;
using System.Text.Json.Nodes;

namespace Imate.API.Presentation.ResponseModels
{
    public class UsageBucketDto
    {
        public long StartTime { get; set; }

        public long EndTime { get; set; }

        public long InputTokens { get; set; }

        public long OutputTokens { get; set; }

        public long TotalTokens => InputTokens + OutputTokens;

        public string? Model { get; set; }

        public string? ProjectId { get; set; }

        public string? UserId { get; set; }

        public string? ApiKeyId { get; set; }

        public string? Service { get; set; }

        public long? Requests { get; set; }
    }

    public class CompletionsUsageResponseDto
    {
        public string? BucketWidth { get; set; }

        public List<UsageBucketDto> Buckets { get; set; } = new();

        public long TotalInputTokens => Buckets.Sum(x => x.InputTokens);

        public long TotalOutputTokens => Buckets.Sum(x => x.OutputTokens);

        public long TotalRequests => Buckets.Sum(x => x.Requests ?? 0);

        public JsonNode? Raw { get; set; }
    }

    public class CostBucketDto
    {
        public long StartTime { get; set; }

        public long EndTime { get; set; }

        public decimal TotalCost { get; set; }

        public string? Currency { get; set; }

        public string? ProjectId { get; set; }
    }

    public class CostsResponseDto
    {
        public string? BucketWidth { get; set; }

        public List<CostBucketDto> Buckets { get; set; } = new();

        public decimal TotalCost => Buckets.Sum(x => x.TotalCost);

        public string? Currency { get; set; }

        public JsonNode? Raw { get; set; }
    }
}

