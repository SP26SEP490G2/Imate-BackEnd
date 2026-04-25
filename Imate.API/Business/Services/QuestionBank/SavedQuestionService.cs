using Imate.API.Business.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels.QuestionBank;

namespace Imate.API.Business.Services.QuestionBank
{
    public class SavedQuestionService : ISavedQuestionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public SavedQuestionService(
            IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<SaveQuestionResponseModel> ToggleSaveQuestionAsync(int accountId, int questionId)
        {
            // Kiểm tra câu hỏi có tồn tại không
            //var question = await _questionRepository.GetByIdAsync(questionId);
            //if (question == null)
            //{
            //    throw new KeyNotFoundException($"Question with ID {questionId} not found.");
            //}

            // Kiểm tra xem đã save chưa
            var existingSave = await _unitOfWork.SavedQuestions
                .GetByAccountAndQuestionAsync(accountId, questionId);

            if (existingSave != null)
            {
                // Đã save rồi → Xóa (unsave)
                await _unitOfWork.SavedQuestions.DeleteAsync(existingSave);

                return new SaveQuestionResponseModel
                {
                    IsSaved = false,
                    Message = "Question unsaved successfully."
                };
            }
            else
            {
                // Chưa save → Thêm mới
                var newSave = new SavedQuestion
                {
                    AccountId = accountId,
                    QuestionId = questionId
                };

                await _unitOfWork.SavedQuestions.AddAsync(newSave);

                return new SaveQuestionResponseModel
                {
                    IsSaved = true,
                    Message = "Question saved successfully."
                };
            }
        }

        //public async Task<IEnumerable<Question>> GetSavedSystemQuestionsAsync(int accountId)
        //{
        //    return await _savedQuestionRepository.GetSavedSystemQuestionsByAccountAsync(accountId);
        //}

        //public async Task<IEnumerable<Question>> GetSavedContributedQuestionsAsync(int accountId)
        //{
        //    return await _savedQuestionRepository.GetSavedContributedQuestionsByAccountAsync(accountId);
        //}

        public async Task<IEnumerable<PublicContributedQuestionResponseModel>> GetSavedContributedQuestionsAsync(int accountId)
        {
            var questions = await _unitOfWork.SavedQuestions.GetSavedContributedQuestionsByAccountAsync(accountId);

            return questions.Select(q => MapToQuestionListResponseModel(q)).ToList();
        }

        public async Task<IEnumerable<PublicSystemQuestionResponseModel>> GetSavedSystemQuestionsAsync(int accountId)
        {
            var questions = await _unitOfWork.SavedQuestions.GetSavedSystemQuestionsByAccountAsync(accountId);

            return questions.Select(q => new PublicSystemQuestionResponseModel
            {
                Id = q.Id,
                Content = q.Content,
                Difficulty = q.Difficulty.ToString(),
                SampleAnswer = q.SampleAnswer,
                CreatorName = q.Creator?.FullName,
                CreatedAt = q.CreatedAt,
                Categories = q.QuestionCategories?.Select(qc => new CategoryDto
                {
                    Id = qc.CategoryId,
                    Name = qc.Category.Name
                }).ToList(),
                Skills = q.QuestionSkills?.Select(qs => new SkillDto
                {
                    Id = qs.SkillId,
                    Name = qs.Skill.Name
                }).ToList(),
                Positions = q.QuestionPositions?.Select(qp => new PositionDto
                {
                    Id = qp.PositionId,
                    Name = qp.Position.Name
                }).ToList(),
                CommentCount = q.Comments.Count,
                IsSaved = true
            }).ToList();
        }

        private PublicContributedQuestionResponseModel MapToQuestionListResponseModel(Question question)
        {
            var response = new PublicContributedQuestionResponseModel
            {
                Id = question.Id,
                Content = question.Content,
                IsActive = question.IsActive,
                SampleAnswer = question.SampleAnswer ?? string.Empty,
                CreatedAt = question.CreatedAt,
                CreatorId = question.CreatorId,
                CreatorName = question.Creator?.FullName ?? "Unknown",
                CreatorAvatarUrl = question.Creator?.AvatarUrl ?? string.Empty,
                CreatorRole = (question.Creator?.AccountRoles?.FirstOrDefault()?.Role?.Name)?.ToString() ?? "Member",

                Categories = question.QuestionCategories?.Select(qc => new CategoryDto
                {
                    Id = qc.CategoryId,
                    Name = qc.Category.Name
                }).ToList(),
                Skills = question.QuestionSkills?.Select(qs => new SkillDto
                {
                    Id = qs.SkillId,
                    Name = qs.Skill.Name
                }).ToList(),
                Positions = question.QuestionPositions?.Select(qp => new PositionDto
                {
                    Id = qp.PositionId,
                    Name = qp.Position.Name
                }).ToList(),

                ContributedDetail = question.ContributedDetail != null ? new Presentation.ResponseModels.QuestionBank.ContributedDetailDto
                {
                    Id = question.ContributedDetail.Id,
                    InterviewDate = question.ContributedDetail.InterviewDate,
                    Level = question.ContributedDetail.Level.ToString(),
                    Company = question.ContributedDetail.Company.Name,
                    CompanyURL = question.ContributedDetail.Company.ImageUrl,
                } : null,
                CommentCount = question.Comments.Count,
                IsSaved = true
                //Comments = question.Comments?
                //    .Select(c => new CommentDto
                //    {
                //        Id = c.Id,
                //        UserId = c.UserId,
                //        UserName = c.User?.FullName ?? "Unknown",
                //        UserAvatarUrl = c.User?.AvatarUrl ?? string.Empty,
                //        Content = c.Content,
                //        CreatedAt = c.CreatedAt,
                //        UpdatedAt = c.UpdatedAt,
                //        UpvoteCount = c.Votes?.Count(v => v.IsUpvote) ?? 0,
                //        DownvoteCount = c.Votes?.Count(v => !v.IsUpvote) ?? 0,
                //        TotalVotes = c.Votes?.Count ?? 0
                //    })
                //    .OrderByDescending(c => c.CreatedAt)
                //    .ToList() ?? new List<CommentDto>(),

                //TotalComments = question.Comments?.Count ?? 0
            };
            return response;
        }
        //Test đến đây rồi
    }
}
