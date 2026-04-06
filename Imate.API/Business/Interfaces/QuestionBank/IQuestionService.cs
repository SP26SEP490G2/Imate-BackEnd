using Imate.API.Business.Helper;
using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels.QuestionBank;
using Imate.API.Presentation.ResponseModels.Classification;
using Imate.API.Presentation.ResponseModels.QuestionBank;

namespace Imate.API.Business.Interfaces.QuestionBank
{
    public interface IQuestionService
    {
        Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync();
        Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request);
        Task<PagedList<GetAllSystemQuestionsForStaffAsyncResponse>> GetAllSystemQuestionsForStaffAsync(GetSystemQuestionParams questionParams);
        Task<PagedList<GetAllContributedQuestionsForStaffAsyncResponse>> GetAllContributedQuestionsForStaffAsync(GetContributedQuestionParams questionParams);
        Task<PagedList<GetAllContributedQuestionsForStaffAsyncResponse>> GetAllPendingContributedQuestionForStaffAsync(PendingContributedParams questionParams);
        Task<List<HiddenQuestion>> GetAllQuestionsHiddenAsync(AllQuestionParams questionParams);
        Task<IEnumerable<PublicSystemQuestionResponseModel>> GetPublicSystemQuestionBanksAsync(string subscription, int? accountId);
        Task<PagedList<PublicSystemQuestionResponseModel>> GetPublicSystemQuestionBanksWithPaginationAsync(string subscription, int? accountId, GetPublicSystemQuestionParams questionParams);
        Task<IEnumerable<PublicContributedQuestionResponseModel>> GetAllPublicContributedQuestionAsync(string subscription,int? accountId);
        Task<PagedList<PublicContributedQuestionResponseModel>> GetPublicContributedQuestionBanksWithPaginationAsync(string subscription, int? accountId, GetPublicContributedQuestionParams questionParams);
        Task<ContributedQuestionDetailsResponseModel> GetPublicContributedQuestionByIdAsync(int questionId, int? accountId);
        Task<Question> CreateSystemQuestionForStaffAsync(CreateSystemQuestionForStaffRequest request, int creatorId);
        Task<Question> UpdateSystemQuestionForStaffAsync(int questionId, UpdateSystemQuestionForStaffRequest request);
        Task<GetAllSystemQuestionsForStaffAsyncResponse> GetSystemQuestionByIdAsync(int questionId, int? accountId);
        Task<GetAllContributedQuestionsForStaffAsyncResponse> GetContributedQuestionByIdAsync(int questionId, int? accountId);

        Task<Question> UpdateContributedQuestionStatusAsync(int questionId, bool status, int staffId);
        Task<Question> ToggleQuestionActiveStatusAsync(int questionId, bool isActive, int staffId);
        Task<List<QuestionValidationResponse>> ValidateQuestionsFromExcelAsync(IFormFile file);
        Task<int> CreateValidatedQuestionsAsync(List<FinalImportRequest> requests, int creatorId);

        //Candidate đóng góp câu hỏi
        Task<ContributionFormDataResponseModel> GetContributionFormDataAsync();
        Task CreateContributedQuestionAsync(ContributeQuestionRequestModel request, int creatorId);
        Task<QuestionValidationResponse> RevalidateSingleQuestionAsync(FinalImportRequest request);
        Task<byte[]> ExportSystemQuestionsToExcelAsync(GetSystemQuestionParams questionParams);
        Task<PagedList<GetMyContributedQuestionsResponse>> GetMyContributedQuestionsAsync(int accountId, GetMyContributedQuestionsParams questionParams);

        // Get positions and skills from questions by company (for manual interview setup)
        Task<CompanyPositionsSkillsResponse> GetPositionsAndSkillsByCompanyAsync(int companyId);

    }
}
