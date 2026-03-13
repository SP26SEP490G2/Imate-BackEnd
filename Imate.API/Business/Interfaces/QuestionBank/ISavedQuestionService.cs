using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels.QuestionBank;

namespace Imate.API.Business.Interfaces.QuestionBank
{
    public interface ISavedQuestionService
    {
        Task<SaveQuestionResponseModel> ToggleSaveQuestionAsync(int accountId, int questionId);
        Task<IEnumerable<PublicSystemQuestionResponseModel>> GetSavedSystemQuestionsAsync(int accountId);
        Task<IEnumerable<PublicContributedQuestionResponseModel>> GetSavedContributedQuestionsAsync(int accountId);

    }
}
