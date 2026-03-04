using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.RequestModels;

namespace Imate.API.Business.Interfaces
{
    public interface IQuestionService
    {
        Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync();
        Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request);
        Task<IEnumerable<QuestionResponse.CategoryItem>> GetListQuestionCategoriesAsync();
    }
}
