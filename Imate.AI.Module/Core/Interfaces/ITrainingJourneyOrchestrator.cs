using Imate.AI.Module.Core.Services;

namespace Imate.AI.Module.Core.Interfaces
{
    public interface ITrainingJourneyOrchestrator
    {
        Task<CreateJourneyResult> CreateJourneyAsync(int accountId, int cvId, string cvContent, string jobDescriptionText, string? journeyName = null);
        Task<StartJourneySessionResult> StartSessionAsync(int accountId, int journeyId);
        Task<EndJourneySessionResult> EndSessionAsync(int accountId, int sessionId);
        Task<JourneyProgressResult> GetProgressAsync(int accountId, int journeyId);
        Task<PaginatedResult<JourneySummaryItem>> GetJourneyListAsync(int accountId, int page, int pageSize);
        Task RenameJourneyAsync(int accountId, int journeyId, string newName);
        // IgnoreGapAsync và RestoreGapAsync đã bị xóa
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class CreateJourneyResult
    {
        public int JourneyId { get; set; }
        public List<string> GapNames { get; set; } = new();
        public int TotalGaps { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class StartJourneySessionResult
    {
        public int SessionId { get; set; }
        public bool AllResolved { get; set; }

        /// <summary>3 gap sẽ luyện trong session — hiển thị cho user trước khi bắt đầu</summary>
        public List<GapPreviewItem> GapsToTrain { get; set; } = new();
    }

    public class GapPreviewItem
    {
        public string GapName { get; set; } = string.Empty;
        public string GapType { get; set; } = string.Empty;

        /// <summary>New | Review</summary>
        public string Mode { get; set; } = "New";
    }

    public class EndJourneySessionResult
    {
        public bool AllResolved { get; set; }
        public List<GapStatusUpdate> GapUpdates { get; set; } = new();
        public string Message { get; set; } = string.Empty;
    }

    public class GapStatusUpdate
    {
        public string GapName { get; set; } = string.Empty;
        public string PreviousStatus { get; set; } = string.Empty;
        public string NewStatus { get; set; } = string.Empty;
        public double Score { get; set; }
        public int ConsecutiveGoodScore { get; set; }
        public int TimesAsked { get; set; }
    }

    public class JourneyProgressResult
    {
        public int JourneyId { get; set; }
        public int UserCvId { get; set; }
        public string JobDescriptionText { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalSessions { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset UpdatedAt { get; set; }
        public List<double?> ScoreHistory { get; set; } = new();

        public List<JourneyGapItem> ResolvedGaps { get; set; } = new();
        public List<JourneyGapItem> UnresolvedGaps { get; set; } = new();

        /// <summary>Thiếu sót kinh nghiệm/học vấn — chỉ hiển thị, không luyện tập</summary>
        public List<string> ProfileGaps { get; set; } = new();

        public List<JourneySessionSummary> SessionHistory { get; set; } = new();
    }

    public class JourneySessionSummary
    {
        public int SessionId { get; set; }
        public int SessionNumber { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public double? EstimatedAbility { get; set; }
        public string? LevelName { get; set; }
        public string SessionGapsJson { get; set; } = "[]";
    }

    public class JourneySummaryItem
    {
        public int JourneyId { get; set; }
        public int UserCvId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string JobDescriptionPreview { get; set; } = string.Empty;
        public int TotalGaps { get; set; }
        public int ResolvedGaps { get; set; }
        public int TotalSessions { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTimeOffset LastPracticed { get; set; }
    }
}