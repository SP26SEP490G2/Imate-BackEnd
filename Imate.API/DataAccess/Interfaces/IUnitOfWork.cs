using Imate.API.DataAccess.Interfaces.Mentors;
using Imate.API.DataAccess.Interfaces.Payment;
using Imate.API.DataAccess.Interfaces.QuestionBank;
using Imate.API.DataAccess.Interfaces.UserManagement;
using Imate.API.Models.Entities;
using Imate.API.DataAccess.Repositories;
using Imate.API.Presentation.ResponseModels;
using Imate.API.Presentation.RequestModels;

namespace Imate.API.DataAccess.Interfaces
{
    public interface IAccountRepository2 : IRepositoryBase<Account>
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

        IAccountRepository Accounts { get; }
        IAccountRepository2 Account { get; }
        IBookingRepository Bookings { get; }
        IUserSubscriptionRepository UserSubscriptions { get; }
        Interfaces.QuestionBank.IQuestionRepository Questions { get; }
        Task SaveChangesAsync();
        IMentorRepository Mentor { get; }
        Interfaces.IQuestionRepository Question { get; }
        ICategoryRepository Category { get; }
        Task SaveAsync();
    }
}
