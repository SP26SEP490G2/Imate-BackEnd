using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.AI.Module.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Imate.API.Business.Interfaces;

namespace Imate.API.Business.Services.ExternalServices
{
    /// <summary>
    /// Triển khai IInterviewSessionDataProvider bằng EF Core.
    /// Map giữa DTO (InterviewSessionData / InterviewResponseData) và Entity.
    /// </summary>
    public class InterviewSessionDataProvider : IInterviewSessionDataProvider
    {
        private readonly ImateDbContext _context;
        private readonly ISystemConfigService _systemConfigService;

        public InterviewSessionDataProvider(ImateDbContext context, ISystemConfigService systemConfigService)
        {
            _context = context;
            _systemConfigService = systemConfigService;
        }

        // ── Session ──

        public async Task<int> CreateSessionAsync(InterviewSessionData data)
        {
            var entity = new InterviewSession
            {
                AccountId = data.AccountId,
                UserCvId = data.UserCvId,
                StartTime = data.StartTime,
                Status = Enum.Parse<InterviewStatus>(data.Status),
                InterviewType = Enum.Parse<InterviewType>(data.InterviewType),
                PositionName = data.PositionName,
                SkillName = data.SkillName,
                LevelName = data.LevelName,
                CompanyName = data.CompanyName,
                JobDescriptionText = data.JobDescriptionText,
                EstimatedAbility = data.EstimatedAbility,
                CvContent = data.CvContent,
                ExtractedSkillsJson = data.ExtractedSkillsJson,
                TrainingJourneyId = data.TrainingJourneyId,
                SessionGapJson = data.SessionGapJson,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.InterviewSessions.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<InterviewSessionData?> GetSessionByIdAsync(int id)
        {
            var entity = await _context.InterviewSessions.FirstOrDefaultAsync(s => s.Id == id);
            return entity == null ? null : MapSessionToDto(entity);
        }

        public async Task UpdateSessionAsync(InterviewSessionData data)
        {
            var entity = await _context.InterviewSessions.FirstOrDefaultAsync(s => s.Id == data.Id);
            if (entity == null) return;

            entity.EndTime = data.EndTime;
            entity.Status = Enum.Parse<InterviewStatus>(data.Status);
            entity.OverallFeedback = data.OverallFeedback;
            entity.EstimatedAbility = data.EstimatedAbility;
            entity.TotalQuestionsAnswered = data.TotalQuestionsAnswered;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<InterviewSessionData>> GetSessionsByAccountIdAsync(int accountId)
        {
            var entities = await _context.InterviewSessions
                .Where(s => s.AccountId == accountId)
                .OrderByDescending(s => s.StartTime)
                .ToListAsync();

            return entities.Select(MapSessionToDto).ToList();
        }

        // ── Response ──

        public async Task<int> CreateResponseAsync(InterviewResponseData data)
        {
            var entity = new InterviewResponse
            {
                InterviewSessionId = data.InterviewSessionId,
                TurnNumber = data.TurnNumber,
                QuestionContent = data.QuestionContent,
                ExpectedAnswerOutline = data.ExpectedAnswerOutline,
                ExpectedBloomLevel = data.ExpectedBloomLevel,
                DifficultyScore = data.DifficultyScore,
                CognitiveLoadScore = data.CognitiveLoadScore,
                Topic = data.Topic,
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.InterviewResponses.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<InterviewResponseData?> GetResponseByIdAsync(int id)
        {
            var entity = await _context.InterviewResponses.FirstOrDefaultAsync(r => r.Id == id);
            return entity == null ? null : MapResponseToDto(entity);
        }

        public async Task UpdateResponseAsync(InterviewResponseData data)
        {
            var entity = await _context.InterviewResponses.FirstOrDefaultAsync(r => r.Id == data.Id);
            if (entity == null) return;

            entity.UserAnswer = data.UserAnswer;
            entity.AnswerTimestamp = data.AnswerTimestamp;
            entity.AIFeedback = data.AIFeedback;
            entity.SuggestedAnswer = data.SuggestedAnswer;
            entity.ExpectedBloomLevel = data.ExpectedBloomLevel;
            entity.DemonstratedBloomLevel = data.DemonstratedBloomLevel;
            entity.BloomScore = data.BloomScore;
            entity.DifficultyScore = data.DifficultyScore;
            entity.CognitiveLoadScore = data.CognitiveLoadScore;
            entity.TechnicalDepthScore = data.TechnicalDepthScore;
            entity.ProblemSolvingScore = data.ProblemSolvingScore;
            entity.CommunicationScore = data.CommunicationScore;
            entity.PracticalExperienceScore = data.PracticalExperienceScore;
            entity.StructuredFeedbackJson = data.StructuredFeedbackJson;
            entity.ExpectedAnswerOutline = data.ExpectedAnswerOutline;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<InterviewResponseData>> GetResponsesBySessionIdAsync(int sessionId)
        {
            var entities = await _context.InterviewResponses
                .Where(r => r.InterviewSessionId == sessionId)
                .OrderBy(r => r.TurnNumber)
                .ToListAsync();

            return entities.Select(MapResponseToDto).ToList();
        }

        // ── Limits & Usage ──

        public async Task<InterviewLimitStatus> GetInterviewLimitStatusAsync(int accountId)
        {
            var now = DateTimeOffset.UtcNow;
            
            // Tìm subscription đang hoạt động
            var activeSub = await _context.UserSubscriptions
                .Include(s => s.Package)
                .Where(s => s.CandidateId == accountId && s.IsActive)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            // Kiểm tra xem subscription có còn hiệu lực về thời gian không
            bool hasValidPaidSub = activeSub != null && activeSub.PackageId != 1;
            if (hasValidPaidSub)
            {
                // Kiểm tra EndDateTime từ CreatedAt + DurationDays
                if (activeSub.Package.DurationDays.HasValue && activeSub.Package.DurationDays.Value > 0)
                {
                    var endDateTime = activeSub.CreatedAt.AddDays(activeSub.Package.DurationDays.Value);
                    if (endDateTime <= now)
                    {
                        hasValidPaidSub = false;
                    }
                }
            }

            if (!hasValidPaidSub)
            {
                // TRƯỜNG HỢP: FREE (Không có sub hoặc sub package 1 hoặc sub hết hạn)
                var freeLimitConfig = await _context.SystemConfigs
                    .FirstOrDefaultAsync(sc => sc.Key == "FREE_INTERVIEW_LIMIT");
                int limit = freeLimitConfig != null && int.TryParse(freeLimitConfig.Value, out var l) ? l : 3;

                // Lấy thông tin Account để lấy số lượt đã dùng
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
                if (account != null)
                {
                    // Kiểm tra reset tháng cho người dùng free
                    await CheckAndResetFreeMonthlyUsageAsync(account);
                }

                int usedInMonth = account?.FreeUsedMock ?? 0;

                return new InterviewLimitStatus
                {
                    IsFree = true,
                    Cost = 1,
                    LimitCount = limit,
                    UsedCount = usedInMonth,
                    RemainingCount = Math.Max(0, limit - usedInMonth),
                    CanStart = usedInMonth < limit,
                    Message = usedInMonth < limit 
                        ? $"Bạn còn {limit - usedInMonth} lượt phỏng vấn miễn phí trong tháng này."
                        : "Bạn đã hết lượt phỏng vấn miễn phí trong tháng này. Hãy nâng cấp gói để tiếp tục!"
                };
            }
            else
            {
                // TRƯỜNG HỢP: PAID SUB
                // Reset số lượt dùng nếu đã sang tháng mới
                await CheckAndResetMonthlyUsageAsync(activeSub);

                // Lấy cost từ system config
                int interviewCost = await _systemConfigService.GetInterviewCostPointsAsync();

                int limit = activeSub.InitialMockLimit;
                int used = activeSub.MockInterviewUsed;
                int remaining = Math.Max(0, limit - used);

                return new InterviewLimitStatus
                {
                    IsFree = false,
                    Cost = interviewCost,
                    LimitCount = limit,
                    UsedCount = used,
                    RemainingCount = remaining,
                    CanStart = remaining >= interviewCost,
                    Message = remaining >= interviewCost
                        ? $"Bạn còn {remaining} lượt phỏng vấn (tốn {interviewCost} lượt/lần) trong gói {activeSub.Package.Name}."
                        : $"Gói {activeSub.Package.Name} của bạn không đủ lượt phỏng vấn (cần {interviewCost} lượt)."
                };
            }
        }

        public async Task IncrementMockInterviewUsageAsync(int accountId)
        {
            var now = DateTimeOffset.UtcNow;
            var activeSub = await _context.UserSubscriptions
                .Include(s => s.Package)
                .Where(s => s.CandidateId == accountId && s.IsActive && s.PackageId != 1)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeSub != null)
            {
                // Chỉ increment nếu sub còn hạn
                bool isValid = true;
                if (activeSub.Package.DurationDays.HasValue && activeSub.Package.DurationDays.Value > 0)
                {
                    var endDateTime = activeSub.CreatedAt.AddDays(activeSub.Package.DurationDays.Value);
                    if (endDateTime <= now) isValid = false;
                }

                if (isValid)
                {
                    // Kiểm tra reset tháng trước khi tăng
                    await CheckAndResetMonthlyUsageAsync(activeSub);

                    // Lấy cost từ system config
                    int interviewCost = await _systemConfigService.GetInterviewCostPointsAsync();

                    activeSub.MockInterviewUsed += interviewCost;
                    activeSub.UpdatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                // TRƯỜNG HỢP: FREE
                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Id == accountId);
                if (account != null)
                {
                    // Kiểm tra reset tháng cho người dùng free
                    await CheckAndResetFreeMonthlyUsageAsync(account);

                    account.FreeUsedMock++;
                    account.UpdatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task CheckAndResetFreeMonthlyUsageAsync(Account account)
        {
            var now = DateTimeOffset.UtcNow;
            var vietnamNow = now.ToOffset(TimeSpan.FromHours(7));

            var lastUpdate = account.UpdatedAt ?? account.CreatedAt;
            var vietnamLastUpdate = lastUpdate.ToOffset(TimeSpan.FromHours(7));

            // Nếu năm hiện tại lớn hơn hoặc (cùng năm nhưng tháng hiện tại lớn hơn)
            if (vietnamNow.Year > vietnamLastUpdate.Year || 
                (vietnamNow.Year == vietnamLastUpdate.Year && vietnamNow.Month > vietnamLastUpdate.Month))
            {
                account.FreeUsedMock = 0;
                account.UpdatedAt = now;
                // Lưu thay đổi được thực hiện bởi caller
            }
        }

        private async Task CheckAndResetMonthlyUsageAsync(UserSubscription sub)
        {
            var now = DateTimeOffset.UtcNow;
            var vietnamNow = now.ToOffset(TimeSpan.FromHours(7));

            var lastUpdate = sub.UpdatedAt ?? sub.CreatedAt;
            var vietnamLastUpdate = lastUpdate.ToOffset(TimeSpan.FromHours(7));

            // Nếu năm hiện tại lớn hơn hoặc (cùng năm nhưng tháng hiện tại lớn hơn)
            if (vietnamNow.Year > vietnamLastUpdate.Year || 
                (vietnamNow.Year == vietnamLastUpdate.Year && vietnamNow.Month > vietnamLastUpdate.Month))
            {
                sub.MockInterviewUsed = 0;
                sub.UpdatedAt = now;
                await _context.SaveChangesAsync();
            }
        }

        // ── Mappers ──

        private static InterviewSessionData MapSessionToDto(InterviewSession entity) => new()
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            UserCvId = entity.UserCvId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            Status = entity.Status.ToString(),
            OverallFeedback = entity.OverallFeedback,
            InterviewType = entity.InterviewType.ToString(),
            QuestionId = entity.QuestionId,
            PositionName = entity.PositionName,
            SkillName = entity.SkillName,
            LevelName = entity.LevelName,
            CompanyName = entity.CompanyName,
            JobDescriptionText = entity.JobDescriptionText,
            EstimatedAbility = entity.EstimatedAbility,
            TotalQuestionsAnswered = entity.TotalQuestionsAnswered,
            CvContent = entity.CvContent,
            ExtractedSkillsJson = entity.ExtractedSkillsJson,
            TrainingJourneyId = entity.TrainingJourneyId,
            SessionGapJson = entity.SessionGapJson
        };

        private static InterviewResponseData MapResponseToDto(InterviewResponse entity) => new()
        {
            Id = entity.Id,
            InterviewSessionId = entity.InterviewSessionId,
            TurnNumber = entity.TurnNumber,
            QuestionContent = entity.QuestionContent,
            UserAnswer = entity.UserAnswer,
            AnswerTimestamp = entity.AnswerTimestamp,
            AIFeedback = entity.AIFeedback,
            SuggestedAnswer = entity.SuggestedAnswer,
            ExpectedBloomLevel = entity.ExpectedBloomLevel,
            DemonstratedBloomLevel = entity.DemonstratedBloomLevel,
            BloomScore = entity.BloomScore,
            DifficultyScore = entity.DifficultyScore,
            CognitiveLoadScore = entity.CognitiveLoadScore,
            IntrinsicLoad = entity.IntrinsicLoad,
            ExtraneousLoad = entity.ExtraneousLoad,
            TechnicalDepthScore = entity.TechnicalDepthScore,
            ProblemSolvingScore = entity.ProblemSolvingScore,
            CommunicationScore = entity.CommunicationScore,
            PracticalExperienceScore = entity.PracticalExperienceScore,
            StarSituationScore = entity.StarSituationScore,
            StarTaskScore = entity.StarTaskScore,
            StarActionScore = entity.StarActionScore,
            StarResultScore = entity.StarResultScore,
            StructuredFeedbackJson = entity.StructuredFeedbackJson,
            ExpectedAnswerOutline = entity.ExpectedAnswerOutline,
            Topic = entity.Topic
        };
    }
}
