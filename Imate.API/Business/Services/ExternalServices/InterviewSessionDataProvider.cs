using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.AI.Module.Interfaces;
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
            ExtractedSkillsJson = entity.ExtractedSkillsJson
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
            ExpectedAnswerOutline = entity.ExpectedAnswerOutline
        };
    }
}
