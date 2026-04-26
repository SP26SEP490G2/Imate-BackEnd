using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Imate.AI.Module.Core.Interfaces;
using Imate.AI.Module.Models.Requests;
using Microsoft.Extensions.Logging;

namespace Imate.AI.Module.Core.Orchestrators
{
    public class TrainingJourneyOrchestrator : ITrainingJourneyOrchestrator
    {
        private readonly ITrainingJourneyDataProvider _journeyProvider;
        private readonly IInterviewSessionDataProvider _sessionProvider;
        private readonly IInterviewAgent _interviewAgent;
        private readonly Services.GapSelectionService _gapSelector;
        private readonly ILogger<TrainingJourneyOrchestrator> _logger;

        public TrainingJourneyOrchestrator(
            ITrainingJourneyDataProvider journeyProvider,
            IInterviewSessionDataProvider sessionProvider,
            IInterviewAgent interviewAgent,
            Services.GapSelectionService gapSelector,
            ILogger<TrainingJourneyOrchestrator> logger)
        {
            _journeyProvider = journeyProvider;
            _sessionProvider = sessionProvider;
            _interviewAgent = interviewAgent;
            _gapSelector = gapSelector;
            _logger = logger;
        }


        public async Task<CreateJourneyResult> CreateJourneyAsync(
            int accountId, int cvId, string cvContent, string jobDescriptionText, string? name = null)
        {
            var existing = await _journeyProvider.FindJourneyAsync(accountId, cvId, jobDescriptionText);
            if (existing != null)
            {
                var existingGaps = DeserializeGaps(existing.GapsJson);
                return new CreateJourneyResult
                {
                    JourneyId = existing.Id,
                    GapNames = existingGaps.Select(g => g.GapName).ToList(),
                    TotalGaps = existingGaps.Count,
                    Message = "Journey đã tồn tại, tiếp tục luyện tập"
                };
            }

            var gapJson = await _interviewAgent.AnalyzeGapsAsync(cvContent, jobDescriptionText);
            var gaps = ParseGapsFromAnalysis(gapJson);

            var journey = new TrainingJourneyData
            {
                AccountId = accountId,
                UserCvId = cvId,
                JobDescriptionText = jobDescriptionText,
                Name = name ?? "Lộ trình không tên",
                GapsJson = JsonSerializer.Serialize(gaps),
                Status = "Pending",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            var journeyId = await _journeyProvider.CreateJourneyAsync(journey);
            _logger.LogInformation("[JOURNEY] Created journey {Id} with {Count} gaps", journeyId, gaps.Count);

            return new CreateJourneyResult
            {
                JourneyId = journeyId,
                GapNames = gaps.Select(g => g.GapName).ToList(),
                TotalGaps = gaps.Count,
                Message = gaps.Any()
                    ? $"Tạo Journey thành công với {gaps.Count} gap cần luyện"
                    : "Tạo Journey thành công. Chúng tôi sẽ trích xuất gap trong quá trình phỏng vấn."
            };
        }

        public async Task<StartJourneySessionResult> StartSessionAsync(int accountId, int journeyId)
        {
            var journey = await _journeyProvider.GetJourneyByIdAsync(journeyId)
                ?? throw new KeyNotFoundException($"Journey {journeyId} không tồn tại");

            if (journey.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập Journey này.");

            var allGaps = DeserializeGaps(journey.GapsJson);
            var selection = _gapSelector.SelectGapsForSession(allGaps);

            if (selection.AllResolved)
                return new StartJourneySessionResult { AllResolved = true, GapsToTrain = new(), SessionId = 0 };

            // Build gap prompt section (null nếu chưa có gap — phiên đầu tiên)
            string? gapPromptSection = allGaps.Any()
                ? _gapSelector.BuildGapPromptSection(selection.SelectedGaps, selection.FillCount)
                : null;

            // Tạo session trực tiếp — không qua InterviewOrchestrator để tránh circular dependency
            var newSession = new InterviewSessionData
            {
                AccountId = accountId,
                UserCvId = journey.UserCvId > 0 ? journey.UserCvId : null,
                JobDescriptionText = journey.JobDescriptionText,
                PositionName = journey.PositionName,
                SkillName = journey.SkillName,
                LevelName = journey.LevelName,
                CompanyName = journey.CompanyName,
                TrainingJourneyId = journeyId,
                SessionGapJson = gapPromptSection,
                Status = "InProgress",
                InterviewType = "FullSession",
                StartTime = DateTimeOffset.UtcNow
            };

            var sessionId = await _sessionProvider.CreateSessionAsync(newSession);

            // Cập nhật LastAskedSessionId cho gap được chọn
            foreach (var gap in selection.SelectedGaps)
                gap.LastAskedSessionId = sessionId;

            journey.GapsJson = JsonSerializer.Serialize(allGaps);
            journey.UpdatedAt = DateTimeOffset.UtcNow;
            journey.TotalSessions++;
            await _journeyProvider.UpdateJourneyAsync(journey);

            _logger.LogInformation("[JOURNEY] Started session {SessionId} for journey {JourneyId}, gaps: {Gaps}",
                sessionId, journeyId,
                selection.SelectedGaps.Any() ? string.Join(", ", selection.SelectedGaps.Select(g => g.GapName)) : "none (first session)");

            return new StartJourneySessionResult
            {
                SessionId = sessionId,
                AllResolved = false,
                GapsToTrain = selection.SelectedGaps.Select(g => new GapPreviewItem
                {
                    GapName = g.GapName,
                    GapType = g.GapType,
                    Mode = g.TimesAsked == 0 ? "New" : "Review"
                }).ToList()
            };
        }

        public async Task<EndJourneySessionResult> EndSessionAsync(int accountId, int sessionId)
        {
            var session = await _sessionProvider.GetSessionByIdAsync(sessionId)
                ?? throw new KeyNotFoundException($"Session {sessionId} không tồn tại");

            if (session.TrainingJourneyId == null)
                throw new InvalidOperationException("Session này không thuộc Journey nào.");

            var journey = await _journeyProvider.GetJourneyByIdAsync(session.TrainingJourneyId.Value)
                ?? throw new KeyNotFoundException("Journey không tồn tại.");

            if (journey.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập Journey này.");

            var allGaps = DeserializeGaps(journey.GapsJson);
            var responses = await _sessionProvider.GetResponsesBySessionIdAsync(sessionId);
            var isFirstSession = journey.TotalSessions <= 1;

            List<string> newGapNames = new();
            List<string> profileGapList = new();

            if (isFirstSession)
            {
                // Phiên đầu tiên: phân tích gap từ CV vs JD + câu trả lời kém
                // Gọi AI AnalyzeGapsAsync để lấy SkillGaps + ProfileGaps
                try
                {
                    var cvContent = session.CvContent ?? string.Empty;
                    var gapJson = await _interviewAgent.AnalyzeGapsAsync(cvContent, journey.JobDescriptionText ?? string.Empty);
                    var newGaps = ParseGapsFromAnalysis(gapJson);

                    // Merge vào allGaps (tránh duplicate)
                    foreach (var g in newGaps)
                    {
                        if (!allGaps.Any(existing => existing.GapName.Equals(g.GapName, StringComparison.OrdinalIgnoreCase)))
                        {
                            allGaps.Add(g);
                            newGapNames.Add(g.GapName);
                        }
                    }

                    // ProfileGaps — parse từ kết quả AI (nếu có field profileGaps)
                    profileGapList = ParseProfileGaps(gapJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[JOURNEY] Lỗi phân tích gap sau phiên đầu tiên");
                }
            }

            // Cập nhật điểm các gap đã được hỏi trong phiên này
            var gapScores = ComputeGapScores(allGaps, responses);
            HandleResolvedGapsDowngrade(allGaps, gapScores);
            _gapSelector.UpdateGapStatuses(allGaps, gapScores, sessionId);

            var allResolved = allGaps.Any() && allGaps.All(g => g.Status == "Resolved");

            // Lưu gap updates
            var profileGapsJson = profileGapList.Any()
                ? JsonSerializer.Serialize(profileGapList)
                : journey.ProfileGapsJson ?? "[]";

            journey.GapsJson = JsonSerializer.Serialize(allGaps);
            journey.UpdatedAt = DateTimeOffset.UtcNow;
            if (allResolved) journey.Status = "Completed";

            await _journeyProvider.UpdateJourneyAsync(journey);
            await _journeyProvider.UpdateJourneyGapsAsync(journey.Id, journey.GapsJson, profileGapsJson);

            _logger.LogInformation("[JOURNEY] EndSession {SessionId}: newGaps={New}, allResolved={AR}",
                sessionId, newGapNames.Count, allResolved);

            return new EndJourneySessionResult
            {
                AllResolved = allResolved,
                GapUpdates = gapScores.Select(gs =>
                {
                    var updatedGap = allGaps.FirstOrDefault(g => g.GapName == gs.GapName);
                    return new GapStatusUpdate
                    {
                        GapName = gs.GapName,
                        PreviousStatus = "Unresolved",
                        NewStatus = updatedGap?.Status ?? "Unresolved",
                        Score = gs.Score,
                        ConsecutiveGoodScore = updatedGap?.ConsecutiveGoodScore ?? 0,
                        TimesAsked = updatedGap?.TimesAsked ?? 0
                    };
                }).ToList(),
                Message = allResolved
                    ? "Chúc mừng! Bạn đã thành thạo tất cả kỹ năng 🎉"
                    : newGapNames.Any()
                        ? $"Phát hiện {newGapNames.Count} kỹ năng cần luyện thêm"
                        : "Đã cập nhật tiến độ luyện tập"
            }; ;
        }

        private void HandleResolvedGapsDowngrade(List<JourneyGapItem> allGaps, List<Services.GapScoreResult> gapScores)
        {
            foreach (var scoreResult in gapScores)
            {
                var gap = allGaps.FirstOrDefault(g =>
                    g.GapName.Equals(scoreResult.GapName, StringComparison.OrdinalIgnoreCase));

                if (gap == null) continue;

                if (gap.Status == "Resolved")
                {
                    if (scoreResult.Score < 0.7)
                    {
                        // Downgrade: từ Resolved → Unresolved
                        var oldConsecutive = gap.ConsecutiveGoodScore;
                        gap.ConsecutiveGoodScore = Math.Max(0, gap.ConsecutiveGoodScore - 1);
                        gap.Status = "Unresolved";

                        _logger.LogWarning(
                            "[JOURNEY-DOWNGRADE] Gap '{Name}' bị downgrade từ Resolved! Score={Score:F2}, " +
                            "ConsecutiveGoodScore: {Old}→{New}, Status: Resolved→Unresolved. " +
                            "Cần 1 lần phỏng vấn tốt khác để khôi phục.",
                            gap.GapName, scoreResult.Score, oldConsecutive, gap.ConsecutiveGoodScore);
                    }
                    else
                    {
                        // Score tốt → giữ nguyên Resolved
                        _logger.LogInformation(
                            "[JOURNEY-MAINTAIN] Gap '{Name}' giữ Resolved. Score={Score:F2}",
                            gap.GapName, scoreResult.Score);
                    }
                }
                else if (gap.Status == "Unresolved")
                {
                    // Unresolved gaps: Không trừ nếu score thấp, chỉ tính lên nếu score tốt
                    // (Việc tính lên được xử lý bởi UpdateGapStatuses)
                    if (scoreResult.Score < 0.7)
                    {
                        _logger.LogInformation(
                            "[JOURNEY-MAINTAIN] Gap '{Name}' vẫn Unresolved. Score={Score:F2} (< 0.7). ConsecutiveGoodScore={Current}",
                            gap.GapName, scoreResult.Score, gap.ConsecutiveGoodScore);
                    }
                    // Nếu score >= 0.7, UpdateGapStatuses sẽ xử lý tăng ConsecutiveGoodScore
                }
            }
        }

        public async Task<JourneyProgressResult> GetProgressAsync(int accountId, int journeyId)
        {
            var journey = await _journeyProvider.GetJourneyByIdAsync(journeyId)
                ?? throw new KeyNotFoundException($"Journey {journeyId} không tồn tại");

            if (journey.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền truy cập Journey này.");

            var summaries = await _journeyProvider.GetSessionSummariesAsync(journeyId);
            var allGaps = DeserializeGaps(journey.GapsJson);
            var profileGaps = DeserializeStringList(journey.ProfileGapsJson ?? "[]");

            return new JourneyProgressResult
            {
                JourneyId = journey.Id,
                UserCvId = journey.UserCvId,
                JobDescriptionText = journey.JobDescriptionText,
                Status = journey.Status,
                TotalSessions = journey.TotalSessions,
                Name = journey.Name,
                UpdatedAt = journey.UpdatedAt,
                ScoreHistory = summaries.Select(s => s.EstimatedAbility).ToList(),
                ResolvedGaps = allGaps.Where(g => g.Status == "Resolved").ToList(),
                UnresolvedGaps = allGaps.Where(g => g.Status == "Unresolved").ToList(),
                ProfileGaps = profileGaps,
                SessionHistory = summaries
                    .Select((s, idx) => new JourneySessionSummary
                    {
                        SessionId = s.SessionId,
                        SessionNumber = idx + 1,
                        StartTime = DateTimeOffset.Parse(s.StartTime).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        EstimatedAbility = s.EstimatedAbility,
                        LevelName = s.LevelName
                    })
                    .OrderByDescending(s => s.SessionNumber)
                    .ToList()
            };
        }

        public async Task RenameJourneyAsync(int accountId, int journeyId, string newName)
        {
            var journey = await _journeyProvider.GetJourneyByIdAsync(journeyId);
            if (journey == null || journey.AccountId != accountId)
                throw new UnauthorizedAccessException("Bạn không có quyền đổi tên lộ trình này.");

            await _journeyProvider.UpdateJourneyNameAsync(journeyId, newName);
            _logger.LogInformation("[JOURNEY] Renamed journey {Id} to {Name}", journeyId, newName);
        }

        public async Task<PaginatedResult<JourneySummaryItem>> GetJourneyListAsync(int accountId, int page, int pageSize)
        {
            var (journeys, totalCount) = await _journeyProvider.GetJourneysPaginatedAsync(accountId, page, pageSize);

            var items = journeys.Select(j =>
            {
                var gaps = DeserializeGaps(j.GapsJson);
                return new JourneySummaryItem
                {
                    JourneyId = j.Id,
                    Name = j.Name,
                    UserCvId = j.UserCvId,
                    JobDescriptionPreview = j.JobDescriptionText.Length > 100
                        ? j.JobDescriptionText.Substring(0, 100) + "..."
                        : j.JobDescriptionText,
                    TotalGaps = gaps.Count(g => g.Status == "Unresolved" || g.Status == "Resolved"),
                    ResolvedGaps = gaps.Count(g => g.Status == "Resolved"),
                    TotalSessions = j.TotalSessions,
                    Status = j.Status,
                    LastPracticed = j.UpdatedAt
                };
            }).ToList();

            return new PaginatedResult<JourneySummaryItem>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        private static List<JourneyGapItem> DeserializeGaps(string json)
        {
            try { return JsonSerializer.Deserialize<List<JourneyGapItem>>(json) ?? new(); }
            catch { return new(); }
        }

        private static List<string> DeserializeStringList(string json)
        {
            try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
            catch { return new(); }
        }

        private static List<JourneyGapItem> ParseGapsFromAnalysis(string gapAnalysisJson)
        {
            var result = new List<JourneyGapItem>();
            try
            {
                var doc = JsonDocument.Parse(gapAnalysisJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("hardSkillsGaps", out var hard))
                    foreach (var item in hard.EnumerateArray())
                        result.Add(new JourneyGapItem { GapName = item.GetString() ?? "", GapType = "hardSkill" });

                if (root.TryGetProperty("experienceGaps", out var exp))
                    foreach (var item in exp.EnumerateArray())
                        result.Add(new JourneyGapItem { GapName = item.GetString() ?? "", GapType = "experience" });

                if (root.TryGetProperty("softSkillsGaps", out var soft))
                    foreach (var item in soft.EnumerateArray())
                        result.Add(new JourneyGapItem { GapName = item.GetString() ?? "", GapType = "softSkill" });
            }
            catch { /* trả về list rỗng nếu parse lỗi */ }

            return result;
        }

        private static List<Services.GapScoreResult> ComputeGapScores(
            List<JourneyGapItem> allGaps,
            List<InterviewResponseData> responses)
        {
            var sessionGaps = allGaps.Where(g => g.LastAskedSessionId != null).ToList();
            if (!sessionGaps.Any()) return new();

            var chunkMappings = new[]
            {
                new { GapIndex = 0, ChunkName = "Technical", TurnMin = 3, TurnMax = 4 },
                new { GapIndex = 1, ChunkName = "Situational", TurnMin = 5, TurnMax = 6 },
                new { GapIndex = 2, ChunkName = "Deep-dive", TurnMin = 7, TurnMax = 8 }
            };

            var result = new List<Services.GapScoreResult>();

            for (int i = 0; i < sessionGaps.Count && i < chunkMappings.Length; i++)
            {
                var gap = sessionGaps[i];
                var mapping = chunkMappings[i];

                // Lấy responses trong chunk đó (dựa trên TurnNumber)
                var chunkResponses = responses.Where(r =>
                    r.TurnNumber >= mapping.TurnMin &&
                    r.TurnNumber <= mapping.TurnMax &&
                    !string.IsNullOrEmpty(r.UserAnswer) &&
                    r.BloomScore.HasValue).ToList();  // ← Chỉ lấy responses có BloomScore

                // FIX: BloomScore là 0-6 scale, cần normalize về 0-1
                // BloomScore 0-6 → divide by 6 để lấy normalized score (0-1)
                // Tuy nhiên: BloomScore có thể đã là 0-1 từ LLM (phụ thuộc vào implementation)
                // Safe approach: Nếu BloomScore > 1, coi như là 0-6 scale
                var avgScore = chunkResponses.Any()
                    ? chunkResponses.Average(r =>
                    {
                        var score = r.BloomScore ?? 0.5;
                        // Normalize: Nếu score > 1, coi như 0-6 scale, chia cho 6
                        // Nếu score <= 1, coi như đã là 0-1 scale
                        return score > 1.0 ? score / 6.0 : score;
                    })
                    : 0.5;

                result.Add(new Services.GapScoreResult
                {
                    GapName = sessionGaps[i].GapName,
                    Score = Math.Round(Math.Clamp(avgScore, 0, 1), 2)
                });

                Console.WriteLine($"[JOURNEY-SCORE] Gap {i} '{gap.GapName}' ({mapping.ChunkName}, turns {mapping.TurnMin}-{mapping.TurnMax}): " +
                    $"{chunkResponses.Count} responses, BloomScores: [{string.Join(", ", chunkResponses.Select(r => r.BloomScore?.ToString("F1") ?? "null"))}], " +
                    $"Normalized: {Math.Round(avgScore, 2)} → {(avgScore >= 0.7 ? "✓ GOOD" : "✗ WEAK")}");
            }

            return result;
        }

        private static List<string> ParseProfileGaps(string gapAnalysisJson)
        {
            var result = new List<string>();
            try
            {
                var doc = JsonDocument.Parse(gapAnalysisJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("profileGaps", out var arr))
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        var s = item.GetString();
                        if (!string.IsNullOrEmpty(s)) result.Add(s);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[v0] ParseProfileGaps error: {ex.Message}");
            }
            return result;
        }
    }
}
