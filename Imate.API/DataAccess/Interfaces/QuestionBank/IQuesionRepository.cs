using Imate.API.Models.Entities;
using Imate.API.Presentation.RequestModels;
using Imate.API.Presentation.ResponseModels;

namespace Imate.API.DataAccess.Interfaces.QuestionBank
{
    public interface IQuestionRepository : IRepositoryBase<Question>
    {
    {
        IQueryable<Question> GetAllSystemQuestionsForStaff();
        IQueryable<Question> GetAllContributedForStaffQuestions();
        Task<IEnumerable<Question>> GetPublicSystemQuestionBanksAsync();
        Task<IEnumerable<Question>> GetAllContributedQuestionsWithRelatedDataAsync();
        Task<Question> GetQuestionByIdWithRelatedDataAsync(int questionId);
        IQueryable<Question> GetAllQuestions();
        //Candidate đóng góp câu hỏi
        Task<IEnumerable<Company>> GetCompaniesAsync();
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<IEnumerable<Position>> GetPositionsWithSkillsAsync();
        Task CreateContributedQuestionAsync(Question question);
        Task<Question> CreateSystemQuestionForStaffAsync(Question question);
        Task<Question> UpdateQuestionAsync(Question question);
        Task<Question> GetQuestionByIdAsync(int questionId);
        Task<IEnumerable<int>> GetSavedQuestionIdsByAccountAsync(int accountId);
        Task<HashSet<string>> FindExistingContentsAsync(List<string> contents);
        Task CreateBulkAsync(IEnumerable<Question> questions);
        Task UpdateRangeAsync(IEnumerable<Question> questions);
        Task SaveChange();
        IQueryable<Question> GetAllQuestionsTracking();
        Task<IEnumerable<Question>> GetLimitedPublicSystemQuestionBanksAsync();
        Task<IEnumerable<Question>> GetLimitedContributedQuestionsWithRelatedDataAsync();
        IQueryable<Question> GetMyContributedQuestions(int accountId);

        //New
        Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync();
        Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request);
    }
}
