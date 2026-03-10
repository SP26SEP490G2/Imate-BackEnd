using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.ResponseModels.Classification;

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
        Task<CompanyPositionsSkillsResponse> GetPositionsAndSkillsByCompanyAsync(int companyId);
        Task<QuestionResponse.GetAllSystemQuestionsForStaffAsyncResponse> GetSystemQuestionByIdAsync(int questionId);
    }
}
