using Imate.AI.Module.Core.Interfaces;
using Imate.API.DataAccess.ApplicationDbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Imate.API.Business.Services
{
    /// <summary>
    /// Bridge giữa Imate.API và AI Module
    /// Cung cấp Question Bank data cho AI Module (RAG)
    /// </summary>
    public class QuestionDataProvider : IQuestionDataProvider
    {
        private readonly ImateDbContext _context;
        private readonly ILogger<QuestionDataProvider> _logger;

        public QuestionDataProvider(ImateDbContext context, ILogger<QuestionDataProvider> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<QuestionBankItem>> GetQuestionsAsync(string positionName, string level, int maxCount = 10)
        {
            _logger.LogInformation(
                "[QuestionDataProvider] GetQuestionsAsync: position={Position}, level={Level}, maxCount={MaxCount}",
                positionName, level, maxCount);

            // Map level sang difficulty
            var difficulty = MapLevelToDifficulty(level);

            // Query questions từ DB
            var query = _context.Questions
                .Where(q => q.IsActive && q.IsFromSystem)
                .Include(q => q.QuestionPositions).ThenInclude(qp => qp.Position)
                .Include(q => q.QuestionSkills).ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionCategories).ThenInclude(qc => qc.Category)
                .AsNoTracking();

            // Filter theo Position nếu có match
            if (!string.IsNullOrWhiteSpace(positionName))
            {
                var positionLower = positionName.ToLower().Trim();
                query = query.Where(q =>
                    q.QuestionPositions.Any(qp =>
                        qp.Position.Name.ToLower().Contains(positionLower) ||
                        positionLower.Contains(qp.Position.Name.ToLower())));
            }

            // Filter theo Difficulty nếu map được
            if (difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == difficulty.Value);
            }

            // Lấy câu hỏi, ưu tiên random để đa dạng
            var questions = await query
                .OrderBy(q => Guid.NewGuid()) // Random order
                .Take(maxCount)
                .ToListAsync();

            _logger.LogInformation("[QuestionDataProvider] Found {Count} questions matching criteria", questions.Count);

            // Nếu filter quá chặt (0 kết quả), fallback: bỏ filter difficulty
            if (questions.Count == 0 && difficulty.HasValue)
            {
                _logger.LogInformation("[QuestionDataProvider] No exact match, falling back without difficulty filter");
                questions = await _context.Questions
                    .Where(q => q.IsActive && q.IsFromSystem)
                    .Where(q => q.QuestionPositions.Any(qp =>
                        qp.Position.Name.ToLower().Contains(positionName.ToLower().Trim()) ||
                        positionName.ToLower().Trim().Contains(qp.Position.Name.ToLower())))
                    .Include(q => q.QuestionPositions).ThenInclude(qp => qp.Position)
                    .Include(q => q.QuestionSkills).ThenInclude(qs => qs.Skill)
                    .Include(q => q.QuestionCategories).ThenInclude(qc => qc.Category)
                    .AsNoTracking()
                    .OrderBy(q => Guid.NewGuid())
                    .Take(maxCount)
                    .ToListAsync();

                _logger.LogInformation("[QuestionDataProvider] Fallback found {Count} questions", questions.Count);
            }

            // Nếu vẫn không có, lấy bất kỳ câu hỏi active nào
            if (questions.Count == 0)
            {
                _logger.LogInformation("[QuestionDataProvider] No position match, getting any active system questions");
                questions = await _context.Questions
                    .Where(q => q.IsActive && q.IsFromSystem)
                    .Include(q => q.QuestionPositions).ThenInclude(qp => qp.Position)
                    .Include(q => q.QuestionSkills).ThenInclude(qs => qs.Skill)
                    .Include(q => q.QuestionCategories).ThenInclude(qc => qc.Category)
                    .AsNoTracking()
                    .OrderBy(q => Guid.NewGuid())
                    .Take(maxCount)
                    .ToListAsync();

                _logger.LogInformation("[QuestionDataProvider] Final fallback found {Count} questions", questions.Count);
            }

            // Map sang DTO
            var result = questions.Select(q => new QuestionBankItem
            {
                Content = q.Content,
                SampleAnswer = q.SampleAnswer,
                Difficulty = q.Difficulty?.ToString() ?? "Unknown",
                Skills = q.QuestionSkills.Select(qs => qs.Skill.Name).ToList(),
                Categories = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
            }).ToList();

            // Log chi tiết từng câu hỏi RAG để verify
            _logger.LogInformation("========== [RAG] KẾT QUẢ TRUY VẤN QUESTION BANK ==========");
            _logger.LogInformation("[RAG] Tổng số câu hỏi lấy từ DB: {Count}/{MaxCount} (position={Position}, level={Level} → difficulty={Difficulty})",
                result.Count, maxCount, positionName, level, difficulty?.ToString() ?? "ALL");
            for (int i = 0; i < result.Count; i++)
            {
                var q = result[i];
                _logger.LogInformation("[RAG] Câu {Index}: [{Difficulty}] {Content}",
                    i + 1, q.Difficulty, q.Content.Length > 120 ? q.Content[..120] + "..." : q.Content);
                if (q.Skills.Count > 0)
                    _logger.LogInformation("[RAG]   → Skills: {Skills}", string.Join(", ", q.Skills));
                if (q.Categories.Count > 0)
                    _logger.LogInformation("[RAG]   → Categories: {Categories}", string.Join(", ", q.Categories));
            }
            _logger.LogInformation("========== [RAG] HẾT KẾT QUẢ TRUY VẤN ==========");

            return result;
        }

        /// <summary>
        /// Map Level (Intern/Fresher/Junior/Middle/Senior) sang DifficultyLevel enum
        /// </summary>
        private static Models.Enums.DifficultyLevel? MapLevelToDifficulty(string level)
        {
            return level?.ToLower().Trim() switch
            {
                "intern" or "fresher" => Models.Enums.DifficultyLevel.Easy,
                "junior" or "middle" => Models.Enums.DifficultyLevel.Medium,
                "senior" => Models.Enums.DifficultyLevel.Hard,
                _ => null
            };
        }
    }
}
