// Imate.API/Business/Services/ExternalServices/TrainingJourneyDataProvider.cs

using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.Entities;
using Imate.API.Models.Enums;
using Imate.AI.Module.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.ExternalServices
{
    public class TrainingJourneyDataProvider : ITrainingJourneyDataProvider
    {
        private readonly ImateDbContext _context;

        public TrainingJourneyDataProvider(ImateDbContext context)
        {
            _context = context;
        }

        public async Task<int> CreateJourneyAsync(TrainingJourneyData data)
        {
            var entity = new TrainingJourney
            {
                AccountId = data.AccountId,
                UserCvId = data.UserCvId,
                JobDescriptionText = data.JobDescriptionText,
                Name = data.Name,
                GapsJson = data.GapsJson,
                ProfileGapsJson = data.ProfileGapsJson ?? "[]",
                Status = TrainingStatus.Pending,
                TotalSessions = 0,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                PositionName = data.PositionName,
                SkillName = data.SkillName,
                LevelName = data.LevelName,
                CompanyName = data.CompanyName
            };

            _context.TrainingJourneys.Add(entity);
            await _context.SaveChangesAsync();
            return entity.Id;
        }

        public async Task<TrainingJourneyData?> GetJourneyByIdAsync(int journeyId)
        {
            var entity = await _context.TrainingJourneys
                .Include(j => j.Sessions)
                .FirstOrDefaultAsync(j => j.Id == journeyId);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<List<TrainingJourneyData>> GetJourneysByAccountIdAsync(int accountId)
        {
            var entities = await _context.TrainingJourneys
                .Where(j => j.AccountId == accountId)
                .Include(j => j.Sessions)
                .OrderByDescending(j => j.UpdatedAt)
                .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        public async Task<TrainingJourneyData?> FindJourneyAsync(int accountId, int cvId, string jobDescriptionText)
        {
            var entity = await _context.TrainingJourneys
                .FirstOrDefaultAsync(j =>
                    j.AccountId == accountId &&
                    j.UserCvId == cvId &&
                    j.JobDescriptionText == jobDescriptionText);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task UpdateJourneyAsync(TrainingJourneyData data)
        {
            var entity = await _context.TrainingJourneys
                .FirstOrDefaultAsync(j => j.Id == data.Id);
            if (entity == null) return;

            entity.GapsJson = data.GapsJson;
            entity.Status = data.Status == "Completed" ? TrainingStatus.Completed : TrainingStatus.Pending;
            entity.TotalSessions = data.TotalSessions;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            entity.SkillName = data.SkillName;
            entity.LevelName = data.LevelName;
            entity.CompanyName = data.CompanyName;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateJourneyGapsAsync(int journeyId, string gapsJson, string profileGapsJson)
        {
            var entity = await _context.TrainingJourneys.FindAsync(journeyId);
            if (entity == null) return;

            entity.GapsJson = gapsJson;
            entity.ProfileGapsJson = profileGapsJson;
            entity.UpdatedAt = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<JourneySessionSummary>> GetSessionSummariesAsync(int journeyId)
        {
            var sessions = await _context.InterviewSessions
                .Where(s => s.TrainingJourneyId == journeyId)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            return sessions.Select((s, index) => new JourneySessionSummary
            {
                SessionId = s.Id,
                SessionNumber = index + 1,
                StartTime = s.StartTime.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                EstimatedAbility = s.EstimatedAbility,
                LevelName = s.LevelName,
                SessionGapsJson = "[]"
            }).ToList();
        }

        public async Task<(List<TrainingJourneyData> Items, int TotalCount)> GetJourneysPaginatedAsync(
            int accountId, int page, int pageSize)
        {
            var query = _context.TrainingJourneys
                .Where(j => j.AccountId == accountId)
                .OrderByDescending(j => j.UpdatedAt);

            var totalCount = await query.CountAsync();
            var entities = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(j => j.Sessions)
                .ToListAsync();

            return (entities.Select(MapToDto).ToList(), totalCount);
        }

        public async Task UpdateJourneyNameAsync(int journeyId, string newName)
        {
            var entity = await _context.TrainingJourneys.FindAsync(journeyId);
            if (entity == null) return;

            entity.Name = newName;
            entity.UpdatedAt = DateTimeOffset.UtcNow;
            await _context.SaveChangesAsync();
        }

        // ── Mapper ──

        private static TrainingJourneyData MapToDto(TrainingJourney entity) => new()
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            UserCvId = entity.UserCvId,
            JobDescriptionText = entity.JobDescriptionText,
            Name = entity.Name ?? "Lộ trình không tên",
            GapsJson = entity.GapsJson,
            ProfileGapsJson = entity.ProfileGapsJson ?? "[]",
            Status = entity.Status == TrainingStatus.Completed ? "Completed" : "Pending",
            TotalSessions = entity.Sessions?.Count ?? entity.TotalSessions,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            PositionName = entity.PositionName,
            SkillName = entity.SkillName,
            LevelName = entity.LevelName,
            CompanyName = entity.CompanyName
        };
    }
}