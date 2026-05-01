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

        public async Task<List<QuestionBankItem>> GetQuestionsAsync(string skillName, string positionName, string level, int maxCount = 10)
        {
            _logger.LogInformation(
                "[QuestionDataProvider] GetQuestionsAsync: skill={Skill}, position={Position}, level={Level}, maxCount={MaxCount}",
                skillName, positionName, level, maxCount);

            // Query questions từ DB
            var query = _context.Questions
                .Where(q => q.IsActive && q.IsFromSystem)
                .Include(q => q.QuestionPositions).ThenInclude(qp => qp.Position)
                .Include(q => q.QuestionSkills).ThenInclude(qs => qs.Skill)
                .Include(q => q.QuestionCategories).ThenInclude(qc => qc.Category)
                .AsNoTracking();

            // Filter theo Skill (ưu tiên chính)
            if (!string.IsNullOrWhiteSpace(skillName))
            {
                var skillLower = skillName.ToLower().Trim();
                query = query.Where(q =>
                    q.QuestionSkills.Any(qs =>
                        qs.Skill.Name.ToLower().Contains(skillLower) ||
                        skillLower.Contains(qs.Skill.Name.ToLower())));
            }

            // Filter theo Position
            if (!string.IsNullOrWhiteSpace(positionName))
            {
                var positionLower = positionName.ToLower().Trim();
                query = query.Where(q =>
                    q.QuestionPositions.Any(qp =>
                        qp.Position.Name.ToLower().Contains(positionLower) ||
                        positionLower.Contains(qp.Position.Name.ToLower())));
            }

            // Filter theo Level nếu có (Level là enum, EF lưu dạng string conversion)
            if (!string.IsNullOrWhiteSpace(level) &&
                Enum.TryParse<Imate.API.Models.Enums.Level>(level, ignoreCase: true, out var levelEnum))
            {
                query = query.Where(q => q.Level != null && q.Level == levelEnum);
            }

            // Lấy câu hỏi, ưu tiên random để đa dạng
            var questions = await query
                .OrderBy(q => Guid.NewGuid()) // Random order
                .Take(maxCount)
                .ToListAsync();

            _logger.LogInformation("[QuestionDataProvider] Found {Count} questions matching criteria (skill + level)", questions.Count);

            // Fallback 1: Nếu filter quá chặt (0 kết quả), bỏ filter level, chỉ giữ skill
            if (questions.Count == 0 && !string.IsNullOrWhiteSpace(skillName))
            {
                _logger.LogInformation("[QuestionDataProvider] No exact match, falling back without level filter");
                var skillLower = skillName.ToLower().Trim();
                questions = await _context.Questions
                    .Where(q => q.IsActive && q.IsFromSystem)
                    .Where(q => q.QuestionSkills.Any(qs =>
                        qs.Skill.Name.ToLower().Contains(skillLower) ||
                        skillLower.Contains(qs.Skill.Name.ToLower())))
                    .Include(q => q.QuestionPositions).ThenInclude(qp => qp.Position)
                    .Include(q => q.QuestionSkills).ThenInclude(qs => qs.Skill)
                    .Include(q => q.QuestionCategories).ThenInclude(qc => qc.Category)
                    .AsNoTracking()
                    .OrderBy(q => Guid.NewGuid())
                    .Take(maxCount)
                    .ToListAsync();

                _logger.LogInformation("[QuestionDataProvider] Fallback (skill only) found {Count} questions", questions.Count);
            }

            // Fallback 2: Nếu vẫn không có, lấy bất kỳ câu hỏi active nào
            if (questions.Count == 0)
            {
                _logger.LogInformation("[QuestionDataProvider] No skill match, getting any active system questions");
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
            _logger.LogInformation("[RAG] Tổng số câu hỏi lấy từ DB: {Count}/{MaxCount} (skill={Skill}, position={Position}, level={Level})",
                result.Count, maxCount, skillName, positionName, level);
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
    }
}
