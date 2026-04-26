using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.AI.Module.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.ExternalServices
{
    /// <summary>
    /// Triển khai IInterviewSessionDataProvider bằng EF Core.
    /// Map giữa DTO (InterviewSessionData / InterviewResponseData) và Entity.
    /// </summary>
    public class InterviewSessionDataProvider : IInterviewSessionDataProvider
    {
        private readonly ImateDbContext _context;

        public InterviewSessionDataProvider(ImateDbContext context)
        {
            _context = context;
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

                // Đếm số interview trong tháng này (giờ VN)
                var vietnamNow = now.ToOffset(TimeSpan.FromHours(7));
                var monthStart = new DateTimeOffset(vietnamNow.Year, vietnamNow.Month, 1, 0, 0, 0, TimeSpan.FromHours(7));
                var nextMonthStart = monthStart.AddMonths(1);

                var usedInMonth = await _context.InterviewSessions
                    .CountAsync(s => s.AccountId == accountId &&
                                    s.StartTime >= monthStart &&
                                    s.StartTime < nextMonthStart);

                return new InterviewLimitStatus
                {
                    IsFree = true,
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

                int limit = activeSub.InitialMockLimit;
                int used = activeSub.MockInterviewUsed;

                return new InterviewLimitStatus
                {
                    IsFree = false,
                    LimitCount = limit,
                    UsedCount = used,
                    RemainingCount = Math.Max(0, limit - used),
                    CanStart = used < limit,
                    Message = used < limit
                        ? $"Bạn còn {limit - used} lượt phỏng vấn trong gói {activeSub.Package.Name}."
                        : $"Gói {activeSub.Package.Name} của bạn đã hết lượt phỏng vấn."
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

                    activeSub.MockInterviewUsed++;
                    activeSub.UpdatedAt = DateTimeOffset.UtcNow;
                    await _context.SaveChangesAsync();
                }
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
