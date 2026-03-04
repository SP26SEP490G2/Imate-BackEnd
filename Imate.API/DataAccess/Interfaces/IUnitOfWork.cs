using Imate.API.Models.Entities;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.RequestModels;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IAccountRepository : IRepositoryBase<Account>
    {
        // Add specific methods here
    }

    public interface IMentorRepository : IRepositoryBase<Mentor>
    {
        Task<IEnumerable<MentorResponse.ListPreviewMentor>> GetListPreviewMentorsAsync();
    }

    public interface IQuestionRepository : IRepositoryBase<Question>
    {
        Task<IEnumerable<QuestionResponse.ListHotQuestion>> GetListHotQuestionsAsync();
        Task<QuestionResponse.QuestionBankList> GetQuestionBankListAsync(QuestionRequest.GetQuestionBankList request);
    }

    public interface ICategoryRepository : IRepositoryBase<Category>
    {
        Task<IEnumerable<QuestionResponse.CategoryItem>> GetListQuestionCategoriesAsync();
    }

    public interface IUnitOfWork
    {
        IAccountRepository Account { get; }
        IMentorRepository Mentor { get; }
        IQuestionRepository Question { get; }
        ICategoryRepository Category { get; }
        Task SaveAsync();
    }
}
