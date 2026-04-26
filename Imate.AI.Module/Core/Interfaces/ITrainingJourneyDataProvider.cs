namespace Imate.AI.Module.Core.Interfaces
{

    public interface ITrainingJourneyDataProvider
    {
        Task<int> CreateJourneyAsync(TrainingJourneyData journey);
        Task<TrainingJourneyData?> GetJourneyByIdAsync(int journeyId);
        Task<List<TrainingJourneyData>> GetJourneysByAccountIdAsync(int accountId);
        Task<TrainingJourneyData?> FindJourneyAsync(int accountId, int cvId, string jobDescriptionText);
        Task UpdateJourneyAsync(TrainingJourneyData journey);
        Task UpdateJourneyNameAsync(int journeyId, string newName);

        /// <summary>Cập nhật GapsJson + ProfileGapsJson sau khi phân tích xong</summary>
        Task UpdateJourneyGapsAsync(int journeyId, string gapsJson, string profileGapsJson);
        Task<List<JourneySessionSummary>> GetSessionSummariesAsync(int journeyId);
        Task<(List<TrainingJourneyData> Items, int TotalCount)> GetJourneysPaginatedAsync(int accountId, int page, int pageSize);
    }

    public class TrainingJourneyData
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int UserCvId { get; set; }
        public string JobDescriptionText { get; set; } = string.Empty;
        public string Name { get; set; } = "Lộ trình không tên";

        /// <summary>Tên vị trí từ SetupInterview — dùng để đặt tên Journey gợi nhớ</summary>
        public string? PositionName { get; set; }
        public string? SkillName { get; set; }
        public string? LevelName { get; set; }
        public string? CompanyName { get; set; }

        /// <summary>JSON List[JourneyGapItem] — kỹ năng cần luyện tập</summary>
        public string GapsJson { get; set; } = "[]";

        /// <summary>
        /// JSON List[string] — thiếu sót kinh nghiệm/học vấn.
        /// Chỉ hiển thị, KHÔNG luyện tập. Phân tích 1 lần sau phiên đầu tiên.
        /// </summary>
        public string ProfileGapsJson { get; set; } = "[]";

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>Pending | Completed</summary>
        public string Status { get; set; } = "Pending";

        public int TotalSessions { get; set; } = 0;
    }

    public class JourneyGapItem
    {
        public string GapName { get; set; } = string.Empty;

        /// <summary>hardSkill | softSkill | experience | extracted</summary>
        public string GapType { get; set; } = "hardSkill";

        /// <summary>Unresolved | Resolved</summary>
        public string Status { get; set; } = "Unresolved";

        /// <summary>
        /// Nguồn gốc gap:
        /// jdRequired   = JD yêu cầu, CV có → ưu tiên hỏi đầu
        /// jdMissing    = JD yêu cầu, CV không có
        /// cvWeak       = CV khai nhưng trả lời kém
        /// extracted    = phát sinh trong phiên phỏng vấn
        /// </summary>
        public string Source { get; set; } = "jdMissing";

        public int TimesAsked { get; set; } = 0;
        public int ConsecutiveGoodScore { get; set; } = 0;
        public int? LastAskedSessionId { get; set; }
    }


    public class SessionGapSelection
    {
        public List<JourneyGapItem> SelectedGaps { get; set; } = new();
        public bool AllResolved { get; set; } = false;
        public bool NeedJdFill { get; set; } = false;
        public int FillCount { get; set; } = 0;
    }
}