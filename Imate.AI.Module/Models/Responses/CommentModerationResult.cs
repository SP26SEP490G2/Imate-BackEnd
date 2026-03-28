namespace Imate.AI.Module.Models.Responses
{
    public class CommentModerationResult
    {
        public bool IsSafe { get; set; }
        public string ViolationCategory { get; set; } = string.Empty;
        public string Reasoning { get; set; } = string.Empty;
        public string SuggestedAction { get; set; } = string.Empty;
    }
}

