using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.Business.Interfaces.QuestionBank
{
    public interface IQuestionService
    {
        Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync();
        Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request);
        Task<IEnumerable<QuestionResponse.QuestionCategoryItem>> GetListQuestionCategoriesAsync();
        Task<PagedList<QuestionResponse.GetAllSystemQuestionsForStaff>> GetAllSystemQuestionsForStaffAsync(QuestionRequest.GetSystemQuestionParams questionParams);
        Task<PagedList<QuestionResponse.GetAllContributedQuestionsForStaff>> GetAllContributedQuestionsForStaffAsync(QuestionRequest.GetContributedQuestionParams questionParams);
        Task<Question> CreateSystemQuestionForStaffAsync(QuestionRequest.CreateSystemQuestionForStaff request);
        Task<Question> UpdateSystemQuestionForStaffAsync(int questionId, QuestionRequest.UpdateSystemQuestionForStaff request);
    }
}
