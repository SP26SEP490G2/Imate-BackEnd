using Google;
using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Classification;
using Imate.API.DataAccess.ApplicationDbContext;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.Classification
{
    public class SkillService : ISkillService
    {
        private readonly ISkillRepository _skillRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ImateDbContext _context;

        public SkillService(ISkillRepository skillRepository, IQuestionRepository questionRepository, IUnitOfWork unitOfWork, ImateDbContext context)
        {
            _skillRepository = skillRepository;
            _questionRepository = questionRepository;
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<List<int>> GetNonExistingSkillIdsAsync(IEnumerable<int> skillIds)
        {
            return await _skillRepository.GetNonExistingSkillIdsAsync(skillIds);
        }
        public async Task<PagedList<SkillResponse>> GetAllSkillsAsync(CommonParams skillParams)
        {
            var query = _skillRepository.GetAllSkills();

            // 1. Filtering: Lọc theo tên
            if (!string.IsNullOrWhiteSpace(skillParams.SearchTerm))
            {
                var searchTerm = skillParams.SearchTerm.ToLower().Trim();
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
            }

            // 2. Filtering: Lọc theo trạng thái IsActive
            if (skillParams.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == skillParams.IsActive.Value);
            }

            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(skillParams.SortBy))
            {
                bool isDescending = skillParams.SortOrder?.ToLower() == "desc";

                query = skillParams.SortBy.ToLower() switch
                {
                    "name" => isDescending
                    ? query.OrderByDescending(q => q.Name.Substring(0, 1).ToLower())
                    : query.OrderBy(q => q.Name.Substring(0, 1).ToLower()),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),

                    _ => throw new NotFoundException($"Invalid SortBy value: {skillParams.SortBy}")
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu
                query = query.OrderBy(q => q.Id);
            }

            var response = query.Select(p => new SkillResponse
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                QuestionCount = _questionRepository.GetAllQuestions()
                                         .Where(q => q.QuestionSkills.Any(q => q.SkillId == p.Id)).Count(),
            });

            return await PagedList<SkillResponse>.CreateAsync(response, skillParams.PageNumber, skillParams.PageSize);
        }
        public async Task<Skill> UpdateSkillsAsync(int id, SkillUpdateRequest skill)
        {
            // 1. Tải (Load): Lấy Skill từ database. DbContext sẽ bắt đầu theo dõi nó.
            var existingSkill = await _skillRepository.GetSkillByIdAsync(id);
            if (existingSkill == null)
            {
                throw new NotFoundException($"Skill with Id {id} not found");
            }

            // 2. Sửa (Modify): Cập nhật các thuộc tính của Skill trong bộ nhớ.
            existingSkill.Name = skill.Name;
            existingSkill.IsActive = skill.IsActive;
            existingSkill.UpdatedAt = DateTime.UtcNow;

            // 3. LOGIC MỚI: Nếu Skill bị vô hiệu hóa, ẩn các Question liên quan.
            if (skill.IsActive == false)
            {
                // Tải tất cả các Question đang active có liên quan đến Skill này.
                // Mối quan hệ: Question -> Position -> PositionSkill -> Skill
                var questionsToUpdate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                              .Where(q => q.QuestionSkills.Any(qc => qc.SkillId == id) && q.IsActive)
                                              .ToListAsync();

                // Cập nhật trạng thái của chúng trong bộ nhớ.
                // EF sẽ tự động theo dõi những thay đổi này.
                foreach (var question in questionsToUpdate)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = false;
                }
            }

            else if (skill.IsActive == true)
            {
                var questionsToCheck = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionSkills.Any(qc => qc.SkillId == id) && !q.IsActive
                                             && q.UpdatedAt != null)
                                             .ToListAsync();

                foreach (var question in questionsToCheck)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = true;


                }
            }

            // 4. Lưu một lần (Save Once): Gọi SaveChangesAsync từ Unit of Work.
            // Lệnh này sẽ lưu tất cả các thay đổi:
            // - Cập nhật `Skill`
            // - Cập nhật các `Question` liên quan
            // Tất cả trong cùng một giao dịch (transaction).
            await _unitOfWork.SaveChangesAsync();

            return existingSkill;
        }
        public async Task<Skill> AddSkillsAsync(SkillCreateRequest skill)
        {
            var newSkill = new Skill
            {
                Name = skill.Name,
                CreatedAt = DateTime.UtcNow,
                IsActive = true

            };
            await _skillRepository.AddSkillAsync(newSkill);
            return newSkill;
        }
        //Test đến đây rồi

        public async Task<List<AffectedQuestionResponseModel>> GetAffectedQuestionsAsync(int skillId, bool willBeActive)
        {
            if (willBeActive)
            {
                // Trả về danh sách rỗng vì kích hoạt không ảnh hưởng tiêu cực
                return new List<AffectedQuestionResponseModel>();
            }

            // Lấy các câu hỏi sẽ bị ẩn
            var affectedQuestions = await _unitOfWork.Questions.GetAllQuestions()
                .Where(q => q.QuestionSkills.Any(qs => qs.SkillId == skillId) && q.IsActive)
                .Select(q => new AffectedQuestionResponseModel
                {
                    Id = q.Id,
                    Content = q.Content,
                    DifficultyLevel = q.Difficulty.HasValue ? q.Difficulty.Value.ToString() : null
                })
                .ToListAsync();

            return affectedQuestions;
        }

        public async Task<Skill?> SetSkillStatusAsync(int id, bool isActive)
        {
            var existingSkill = await _skillRepository.GetSkillByIdAsync(id);
            if (existingSkill == null)
            {
                return null;
            }

            existingSkill.IsActive = isActive;
            existingSkill.UpdatedAt = DateTime.UtcNow;

            // Logic tương tự UpdateSkillsAsync để cập nhật questions liên quan
            if (isActive == false)
            {
                var questionsToUpdate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                              .Where(q => q.QuestionSkills.Any(qc => qc.SkillId == id) && q.IsActive)
                                              .ToListAsync();

                foreach (var question in questionsToUpdate)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = false;
                }
            }
            else if (isActive == true)
            {
                var questionsToCheck = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionSkills.Any(qc => qc.SkillId == id) && !q.IsActive
                                             && q.UpdatedAt != null)
                                             .ToListAsync();

                foreach (var question in questionsToCheck)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = true;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return existingSkill;
        }
    }
}
