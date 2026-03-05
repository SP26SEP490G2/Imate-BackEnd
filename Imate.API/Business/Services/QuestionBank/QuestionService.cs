using Imate.API.Business.Exceptions;
using Imate.API.Business.Helper;
using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Models.Entities;
using Imate.API.Models.Enums;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;
using Microsoft.EntityFrameworkCore;

namespace Imate.API.Business.Services.QuestionBank
{
    public class QuestionService : IQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public QuestionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync()
        {
            try
            {
                return await _unitOfWork.Questions.GetListHotQuestionsAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving hot questions.", ex);
            }
        }

        public async Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request)
        {
            try
            {
                var query = _unitOfWork.Questions.GetQuestionBankListAsync();
                // Apply search filter
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    query = query.Where(q => q.Content.Contains(request.SearchTerm));
                }

                // Apply category filter
                if (request.CategoryId.HasValue)
                {
                    query = query.Where(q => q.QuestionCategories.Any(qc => qc.CategoryId == request.CategoryId.Value));
                }

                // Apply difficulty filter
                if (!string.IsNullOrWhiteSpace(request.Difficulty))
                {
                    query = query.Where(q => q.Difficulty.ToString() == request.Difficulty);
                }

                // Apply sorting
                query = request.SortBy?.ToLower() switch
                {
                    "oldest" => query.OrderBy(q => q.CreatedAt),
                    "mostcommented" => query.OrderByDescending(q => q.Comments.Count),
                    _ => query.OrderByDescending(q => q.CreatedAt) // newest
                };

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize);

                var questions = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(q => new QuestionResponse.QuestionBankItem
                    {
                        Id = q.Id,
                        Title = q.Content.Length > 50 ? q.Content.Substring(0, 50) + "..." : q.Content,
                        Content = q.Content,
                        Categories = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                        Skills = q.QuestionSkills.Select(qs => qs.Skill.Name).ToList(),
                        Difficulty = q.Difficulty.HasValue ? q.Difficulty.ToString() : null,
                        CommentCount = q.Comments.Count,
                        CreatedBy = q.Creator.FullName,
                        CreatedAt = q.CreatedAt
                    })
                    .ToListAsync();
                return new QuestionResponse.QuestionBankList
                {
                    Questions = questions,
                    TotalCount = totalCount,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    TotalPages = totalPages
                };
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving question bank list.", ex);
            }
        }

        public async Task<IEnumerable<QuestionResponse.QuestionCategoryItem>> GetListQuestionCategoriesAsync()
        {
            try
            {
                return await _unitOfWork.Questions.GetListQuestionCategoriesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving question categories.", ex);
            }
        }
        public async Task<PagedList<QuestionResponse.GetAllSystemQuestionsForStaff>> GetAllSystemQuestionsForStaffAsync(QuestionRequest.GetSystemQuestionParams questionParams)
        {

            var query = _unitOfWork.Questions.GetAllSystemQuestionsForStaff();
            // 1. Filter theo SearchTerm (trên Content)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm));
            }

            // 2. Filter theo Trạng thái (IsActive)
            if (questionParams.IsActive.HasValue)
            {
                query = query.Where(q => q.IsActive == questionParams.IsActive.Value);
            }

            // 3. Filter theo Skill ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.SkillId.HasValue)
            {
                // Lấy những câu hỏi có chứa SkillId được cung cấp
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 4. Filter theo Position ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }
            // 5. Filter theo Category ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qp => qp.CategoryId == questionParams.CategoryId.Value));
            }
            // 5. Filter theo Difficulty (Level)
            if (questionParams.Difficulty.HasValue)
            {
                query = query.Where(q => q.Difficulty == questionParams.Difficulty.Value);
            }

            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";

                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
    ? query.OrderByDescending(q => q.Content.Substring(0, 1).ToLower())
    : query.OrderBy(q => q.Content.Substring(0, 1).ToLower()),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),

                    _ => throw new NotFoundException($"Invalid SortBy value: {questionParams.SortBy}")
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu
                query = query.OrderByDescending(q => q.CreatedAt);
            }
            var response = query.Select(q => new QuestionResponse.GetAllSystemQuestionsForStaff
            {
                Id = q.Id,
                Content = q.Content,
                Difficulty = q.Difficulty,
                IsFromSystem = q.IsFromSystem,
                IsActive = q.IsActive,
                CreatorId = q.CreatorId,
                CreatorName = q.Creator.FullName,
                SampleAnswer = q.SampleAnswer,
                CategoriesName = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                SkillsName = q.QuestionSkills.Select(qs => qs.Skill.Name).ToList(),
                PositionsName = q.QuestionPositions.Select(qp => qp.Position.Name).ToList()
            });
            return await PagedList<QuestionResponse.GetAllSystemQuestionsForStaff>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);

        }
        public async Task<PagedList<QuestionResponse.GetAllContributedQuestionsForStaff>> GetAllContributedQuestionsForStaffAsync(QuestionRequest.GetContributedQuestionParams questionParams)
        {
            var query = _unitOfWork.Questions.GetAllContributedForStaffQuestions();

            // Filter: Chỉ hiển thị các câu hỏi đã được approve (ApprovalStatus = Approved)
            // Loại bỏ các câu hỏi đang chờ duyệt (Pending) và các câu hỏi bị reject (Rejected)
            query = query.Where(q => q.ApprovalStatus == QuestionApprovalStatus.Approved);

            // 1. Filter theo SearchTerm (trên Content)
            if (!string.IsNullOrWhiteSpace(questionParams.SearchTerm))
            {
                var searchTerm = questionParams.SearchTerm.ToLower().Trim();
                query = query.Where(q => q.Content.ToLower().Contains(searchTerm));
            }

            // 2. Filter theo Trạng thái (IsActive) - Optional, có thể filter thêm nếu cần
            if (questionParams.IsActive.HasValue)
            {
                query = query.Where(q => q.IsActive == questionParams.IsActive.Value);
            }

            // 3. Filter theo Skill ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.SkillId.HasValue)
            {
                // Lấy những câu hỏi có chứa SkillId được cung cấp
                query = query.Where(q => q.QuestionSkills.Any(qs => qs.SkillId == questionParams.SkillId.Value));
            }

            // 4. Filter theo Position ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.PositionId.HasValue)
            {
                query = query.Where(q => q.QuestionPositions.Any(qp => qp.PositionId == questionParams.PositionId.Value));
            }
            // 5. Filter theo Category ID (many-to-many, nhưng chỉ với 1 ID)
            if (questionParams.CategoryId.HasValue)
            {
                query = query.Where(q => q.QuestionCategories.Any(qp => qp.CategoryId == questionParams.CategoryId.Value));
            }
            // 5. Filter theo Company ID
            if (questionParams.CompanyId.HasValue)
            {
                query = query.Where(q => q.ContributedDetail.CompanyId == questionParams.CompanyId.Value);
            }

            // 6. Filter theo Difficulty (Level)
            if (questionParams.Level.HasValue)
            {
                query = query.Where(q => q.ContributedDetail.Level == questionParams.Level.Value);
            }


            // --- LOGIC SẮP XẾP (SORTING) ---
            // Luôn phải có một thứ tự sắp xếp để phân trang hoạt động chính xác
            if (!string.IsNullOrWhiteSpace(questionParams.SortBy))
            {
                bool isDescending = questionParams.SortOrder?.ToLower() == "desc";

                query = questionParams.SortBy.ToLower() switch
                {
                    "content" => isDescending
    ? query.OrderByDescending(q => q.Content.Substring(0, 1).ToLower())
    : query.OrderBy(q => q.Content.Substring(0, 1).ToLower()),
                    "createdat" => isDescending
                        ? query.OrderByDescending(q => q.CreatedAt)
                        : query.OrderBy(q => q.CreatedAt),
                };
            }
            else
            {
                // Sắp xếp mặc định khi không có yêu cầu
                query = query.OrderByDescending(q => q.CreatedAt);
            }

            var response = query.Select(q => new QuestionResponse.GetAllContributedQuestionsForStaff
            {
                Id = q.Id,
                Content = q.Content ?? string.Empty,
                Difficulty = q.Difficulty,
                IsFromSystem = q.IsFromSystem,
                IsActive = q.IsActive,
                CreatorId = q.CreatorId,
                CreatorName = q.Creator.FullName ?? string.Empty,
                SampleAnswer = q.SampleAnswer,
                ContributedDetailId = q.ContributedDetailId,
                ContributedDetail = q.ContributedDetail,
                CategoriesName = q.QuestionCategories.Select(qc => qc.Category.Name).ToList(),
                SkillsName = q.QuestionSkills.Select(qs => qs.Skill.Name ?? string.Empty).ToList(),
                PositionsName = q.QuestionPositions.Select(qp => qp.Position.Name ?? string.Empty).ToList()

            });

            return await PagedList<QuestionResponse.GetAllContributedQuestionsForStaff>.CreateAsync(response, questionParams.PageNumber, questionParams.PageSize);
        }

        public async Task<Question> CreateSystemQuestionForStaffAsync(QuestionRequest.CreateSystemQuestionForStaff request)
        {
            // 1. Ánh xạ thuộc tính cơ bản
            var question = new Question
            {
                Content = request.Content,
                Difficulty = request.Difficulty,
                SampleAnswer = request.SampleAnswer,
                CreatorId = request.CreatorId,
                IsFromSystem = true,
                IsActive = true
            };
            var a = new List<int>();
            // 2. Kiểm tra tồn tại của Creator ID
            if (await _unitOfWork.Accounts.AreUsersExisted(request.CreatorId) == false)
                throw new NotFoundException($"Creator ID {request.CreatorId} không tồn tại.");

            // --- BẮT ĐẦU PHẦN ÁNH XẠ VÀ VALIDATION CHO CÁC MỐI QUAN HỆ ---

            // 3. Ánh xạ và Validate Category IDs
            if (request.CategoryIds?.Any() == true)
            {
                // 3a. Validate: Kiểm tra từng ID (Nên tối ưu thành 1 Query như đã thảo luận)
                a = await _unitOfWork.Categories.GetNonExistingCategoryIdsAsync(request.CategoryIds);
                if (a.Any())
                    throw new NotFoundException($"Category ID {string.Join(", ", a)} không tồn tại.");


                // 3b. ÁNH XẠ: Tạo Collection các QuestionCategory
                question.QuestionCategories = request.CategoryIds
                    .Select(cId => new QuestionCategory
                    {
                        CategoryId = cId,
                        Question = question // Liên kết ngược (nếu cần thiết cho EF Core)
                    })
                    .ToList();
            }

            // 4. Ánh xạ và Validate Skill IDs
            if (request.SkillIds?.Any() == true)
            {
                a = await _unitOfWork.Skills.GetNonExistingSkillIdsAsync(request.SkillIds);
                if (a.Any())
                    throw new NotFoundException($"Skill ID {string.Join(", ", a)} không tồn tại.");


                // 4b. ÁNH XẠ: Tạo Collection các QuestionSkill
                question.QuestionSkills = request.SkillIds
                    .Select(sId => new QuestionSkill
                    {
                        SkillId = sId,
                        Question = question
                    })
                    .ToList();
            }

            // 5. Ánh xạ và Validate Position IDs
            if (request.PositionIds?.Any() == true)
            {
                a = await _unitOfWork.Positions.GetNonExistingPositionIdsAsync(request.PositionIds);
                if (a.Any())
                    throw new NotFoundException($"Position ID {string.Join(", ", a)} không tồn tại.");


                // 5b. ÁNH XẠ: Tạo Collection các QuestionPosition
                question.QuestionPositions = request.PositionIds
                    .Select(pId => new QuestionPosition
                    {
                        PositionId = pId,
                        Question = question
                    })
                    .ToList();
            }

            // --- KẾT THÚC ÁNH XẠ ---

            // 6. Lưu vào DB (EF Core sẽ tự động thêm Question và tất cả các Collection con)
            var created = await _unitOfWork.Questions.CreateSystemQuestionForStaffAsync(question);

            return created;
        }

        public async Task<Question> UpdateSystemQuestionForStaffAsync(int questionId, QuestionRequest.UpdateSystemQuestionForStaff request)
        {
            // 1. Tìm câu hỏi gốc
            var questionToUpdate = await _unitOfWork.Questions.GetQuestionByIdAsync(questionId);
            if (questionToUpdate == null)
            {
                throw new NotFoundException($"Không tìm thấy câu hỏi hệ thống với ID {questionId}.");
            }

            // 2. KIỂM TRA SỰ TỒN TẠI CỦA ID (Phiên bản nâng cấp)
            // Thay thế logic cũ bằng logic mới ở đây

            // --- Kiểm tra Categories ---
            var nonExistingCategoryIds = await _unitOfWork.Categories.GetNonExistingCategoryIdsAsync(request.CategoryIds);
            if (nonExistingCategoryIds.Any())
            {
                var invalidIdsString = string.Join(", ", nonExistingCategoryIds);
                throw new BadRequestException($"Các CategoryId sau không tồn tại: {invalidIdsString}.");
            }

            // --- Kiểm tra Skills ---
            var nonExistingSkillIds = await _unitOfWork.Skills.GetNonExistingSkillIdsAsync(request.SkillIds);
            if (nonExistingSkillIds.Any())
            {
                var invalidIdsString = string.Join(", ", nonExistingSkillIds);
                throw new BadRequestException($"Các SkillId sau không tồn tại: {invalidIdsString}.");
            }

            // --- Kiểm tra Positions ---
            var nonExistingPositionIds = await _unitOfWork.Positions.GetNonExistingPositionIdsAsync(request.PositionIds);
            if (nonExistingPositionIds.Any())
            {
                var invalidIdsString = string.Join(", ", nonExistingPositionIds);
                throw new BadRequestException($"Các PositionId sau không tồn tại: {invalidIdsString}.");
            }

            // 3. Cập nhật các thuộc tính và quan hệ
            // (Giữ nguyên logic mapping và update relationship)
            questionToUpdate.Content = request.Content;
            questionToUpdate.SampleAnswer = request.SampleAnswer;
            questionToUpdate.Difficulty = request.Difficulty;
            questionToUpdate.IsActive = request.IsActive;
            questionToUpdate.UpdatedAt = DateTime.UtcNow;
            UpdateQuestionRelationships(questionToUpdate, request, questionId);

            // 4. Lưu thay đổi
            await _unitOfWork.Questions.UpdateQuestionAsync(questionToUpdate);

            // 5. Trả về đối tượng đã cập nhật khi thành công
            return questionToUpdate;
        }

        private void UpdateQuestionRelationships(Question questionToUpdate, QuestionRequest.UpdateSystemQuestionForStaff request, int questionId)
        {
            // --- Categories ---
            var categoryIdsInRequest = request.CategoryIds ?? new List<int>();

            // SỬA LỖI Ở ĐÂY:
            // 1. Tìm tất cả các mục cần xóa
            var categoriesToRemove = questionToUpdate.QuestionCategories
                .Where(qc => !categoryIdsInRequest.Contains(qc.CategoryId))
                .ToList();

            // 2. Duyệt qua danh sách tạm và xóa khỏi collection gốc
            foreach (var categoryToRemove in categoriesToRemove)
            {
                questionToUpdate.QuestionCategories.Remove(categoryToRemove);
            }

            // Phần thêm mới giữ nguyên
            var currentCategoryIds = questionToUpdate.QuestionCategories.Select(qc => qc.CategoryId).ToList();
            var categoryIdsToAdd = categoryIdsInRequest.Except(currentCategoryIds).ToList();
            foreach (var categoryId in categoryIdsToAdd)
            {
                questionToUpdate.QuestionCategories.Add(new QuestionCategory { QuestionId = questionId, CategoryId = categoryId });
            }

            // --- Skills (áp dụng cách sửa tương tự) ---
            var skillIdsInRequest = request.SkillIds ?? new List<int>();

            var skillsToRemove = questionToUpdate.QuestionSkills
                .Where(qs => !skillIdsInRequest.Contains(qs.SkillId))
                .ToList();

            foreach (var skillToRemove in skillsToRemove)
            {
                questionToUpdate.QuestionSkills.Remove(skillToRemove);
            }

            var currentSkillIds = questionToUpdate.QuestionSkills.Select(qs => qs.SkillId).ToList();
            var skillIdsToAdd = skillIdsInRequest.Except(currentSkillIds).ToList();
            foreach (var skillId in skillIdsToAdd)
            {
                questionToUpdate.QuestionSkills.Add(new QuestionSkill { QuestionId = questionId, SkillId = skillId });
            }

            // --- Positions (áp dụng cách sửa tương tự) ---
            var positionIdsInRequest = request.PositionIds ?? new List<int>();

            var positionsToRemove = questionToUpdate.QuestionPositions
                .Where(qp => !positionIdsInRequest.Contains(qp.PositionId))
                .ToList();

            foreach (var positionToRemove in positionsToRemove)
            {
                questionToUpdate.QuestionPositions.Remove(positionToRemove);
            }

            var currentPositionIds = questionToUpdate.QuestionPositions.Select(qp => qp.PositionId).ToList();
            var positionIdsToAdd = positionIdsInRequest.Except(currentPositionIds).ToList();
            foreach (var positionId in positionIdsToAdd)
            {
                questionToUpdate.QuestionPositions.Add(new QuestionPosition { QuestionId = questionId, PositionId = positionId });
            }
        }

    }
}
