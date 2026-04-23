using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.Classification;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.Classification;
using Imate.API.Presentation.ResponseModels.Classification;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.Classification
{
    public class PositionService : IPositionService
    {
        private readonly IPositionRepository _positionRepository;
        private readonly ISkillRepository _skillRepository;
        private readonly IQuestionRepository _questionRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PositionService(IPositionRepository positionRepository, ISkillRepository skillRepository, IQuestionRepository questionRepository, IUnitOfWork unitOfWork)
        {
            _positionRepository = positionRepository;
            _skillRepository = skillRepository;
            _questionRepository = questionRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<List<int>> GetNonExistingPositionIdsAsync(IEnumerable<int> positionIds)
        {
            return await _positionRepository.GetNonExistingPositionIdsAsync(positionIds);
        }
        public async Task<PagedList<PositionResponse>> GetAllPositionsAsync(CommonParams positionParams)
        {
            var query = _positionRepository.GetAllPositions();

            // 1. Filtering: Lọc theo tên
            if (!string.IsNullOrWhiteSpace(positionParams.SearchTerm))
            {
                var searchTerm = positionParams.SearchTerm.ToLower().Trim();
                query = query.Where(c => c.Name.ToLower().Contains(searchTerm));
            }
            // 2. Filtering: Lọc theo trạng thái
            if (positionParams.IsActive.HasValue)
            {
                // Nếu có, thì thêm điều kiện Where vào câu query
                query = query.Where(c => c.IsActive == positionParams.IsActive.Value);
            }
            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(positionParams.SortBy))
            {
                bool isDescending = positionParams.SortOrder?.ToLower() == "desc";

                query = positionParams.SortBy.ToLower() switch
                {
                    "name" => isDescending
    ? query.OrderByDescending(q => q.Name)
    : query.OrderBy(q => q.Name),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),

                    _ => throw new NotFoundException($"Invalid SortBy value: {positionParams.SortBy}")
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu
                query = query.OrderBy(q => q.Id);
            }
            var reponse = query.Select(p => new PositionResponse
            {
                Id = p.Id,
                Name = p.Name,
                IsActive = p.IsActive,
                QuestionCount = _questionRepository.GetAllQuestions()
                                  .Where(q => q.QuestionPositions.Any(q => q.PositionId == p.Id)).Count()
            });


            // 3. Paging: Tạo PagedList từ query đã được xử lý
            // NOTE: Chúng ta đang trả về trực tiếp Entity "Category" theo yêu cầu của bạn.
            // Lý tưởng nhất, chúng ta nên trả về PagedList<CategoryDto>.
            return await PagedList<PositionResponse>.CreateAsync(reponse, positionParams.PageNumber, positionParams.PageSize);
        }
        public async Task<Position> AddPositionAsync(PositionCreateRequest position)
        {
            var newPosition = new Position
            {
                Name = position.Name,
                CreatedAt = DateTimeOffset.UtcNow,
                IsActive = true
            };
            return await _positionRepository.AddPositionAsync(newPosition);
        }
        public async Task<Position> UpdatePositionAsync(int id, PositionUpdateRequest positionUpdate)
        {
            // 1. Tải Position và các Skill liên quan qua Repository.
            // PHẢI ĐẢM BẢO `GetPositionByIdAsync` có khả năng .Include() hoặc đã tải sẵn PositionSkills.
            var existingPosition = await _positionRepository.GetPositionByIdAsync(id);
            if (existingPosition == null)
            {
                throw new NotFoundException($"Không tìm thấy Position với Id: {id}");
            }

            // 3. Cập nhật các thuộc tính của Position trong bộ nhớ.
            // EF đang theo dõi đối tượng này và sẽ tự động phát hiện thay đổi.
            existingPosition.Name = positionUpdate.Name;
            existingPosition.IsActive = positionUpdate.IsActive;
            existingPosition.UpdatedAt = DateTime.UtcNow;

            // 5. 💡 LOGIC MỚI: Dùng QuestionRepository để lấy dữ liệu
            if (positionUpdate.IsActive == false)
            {
                // Tải các question liên quan thông qua repository của nó.
                var questionsToDeactivate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionPositions.Any(qc => qc.PositionId == id) && q.IsActive)
                                             .ToListAsync();

                // Cập nhật trạng thái của chúng trong bộ nhớ.
                foreach (var question in questionsToDeactivate)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = false;
                }
            }

            else if (positionUpdate.IsActive == true)
            {
                var questionsToCheck = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionPositions.Any(qc => qc.PositionId == id) && !q.IsActive && q.UpdatedAt != null)
                                             .ToListAsync();

                foreach (var question in questionsToCheck)
                {
                    //var allCategoriesActive = question.QuestionCategories.All(qc => qc.Category.IsActive);
                    //var allSkillsActive = question.QuestionSkills.All(qs => qs.Skill.IsActive);
                    //var allPositionsActive = question.QuestionPositions.All(qp => qp.Position.IsActive);

                    //if (allCategoriesActive && allSkillsActive && allPositionsActive)
                    //{
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = true;
                    //}

                }
            }

            // 6. 🔑 LƯU MỘT LẦN DUY NHẤT QUA UNIT OF WORK
            // Bỏ hoàn toàn lệnh `_positionRepository.UpdatePositionAsync()`.
            // Lệnh SaveChangesAsync từ UnitOfWork sẽ lưu tất cả các thay đổi
            // mà DbContext đã theo dõi từ CẢ HAI repository.
            await _unitOfWork.SaveChangesAsync();

            return existingPosition;
        }
        //Test đến đây rồi


        public async Task<List<AffectedQuestionResponseModel>> GetAffectedQuestionsAsync(int positionId, bool willBeActive)
        {
            if (willBeActive)
            {
                // Trả về danh sách rỗng vì kích hoạt không ảnh hưởng tiêu cực
                return new List<AffectedQuestionResponseModel>();
            }

            // Lấy các câu hỏi sẽ bị ẩn
            var affectedQuestions = await _unitOfWork.Questions.GetAllQuestions()
                 .Where(q => q.QuestionPositions.Any(qp => qp.PositionId == positionId) && q.IsActive)
                .Select(q => new AffectedQuestionResponseModel
                {
                    Id = q.Id,
                    Content = q.Content,
                    DifficultyLevel = q.Difficulty.HasValue ? q.Difficulty.Value.ToString() : null
                })
                .ToListAsync();

            return affectedQuestions;
        }

        public async Task<Position?> SetPositionStatusAsync(int id, bool isActive)
        {
            var existingPosition = await _positionRepository.GetPositionByIdAsync(id);
            if (existingPosition == null)
            {
                return null;
            }

            existingPosition.IsActive = isActive;
            existingPosition.UpdatedAt = DateTime.UtcNow;

            // Logic tương tự UpdatePositionAsync để cập nhật questions liên quan
            if (isActive == false)
            {
                var questionsToDeactivate = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionPositions.Any(qc => qc.PositionId == id) && q.IsActive)
                                             .ToListAsync();

                foreach (var question in questionsToDeactivate)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = false;
                }
            }
            else if (isActive == true)
            {
                var questionsToCheck = await _unitOfWork.Questions.GetAllQuestionsTracking()
                                             .Where(q => q.QuestionPositions.Any(qc => qc.PositionId == id) && !q.IsActive && q.UpdatedAt != null)
                                             .ToListAsync();

                foreach (var question in questionsToCheck)
                {
                    question.UpdatedAt = DateTime.UtcNow;
                    question.IsActive = true;
                }
            }

            await _unitOfWork.SaveChangesAsync();
            return existingPosition;
        }
    }
}
