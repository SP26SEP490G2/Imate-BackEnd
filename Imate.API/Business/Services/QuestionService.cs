using Imate.API.Business.Interfaces;
using Imate.API.DataAccess.Interfaces;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.RequestModels;

namespace Imate.API.Business.Services
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
                return await _unitOfWork.Question.GetListHotQuestionsAsync();
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
                return await _unitOfWork.Question.GetQuestionBankListAsync(request);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving question bank list.", ex);
            }
        }

        public async Task<IEnumerable<QuestionResponse.CategoryItem>> GetListQuestionCategoriesAsync()
        {
            try
            {
                return await _unitOfWork.Category.GetListQuestionCategoriesAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("An error occurred while retrieving question categories.", ex);
            }
        }
    }
}
